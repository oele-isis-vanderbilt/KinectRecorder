using FFMediaToolkit;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;
using System.Text;

public class KinectManager : MonoBehaviour {

    public int FrameRate = 30;
    public RawImage _colorImage;
    public RawImage _depthImage;
    public TextMeshProUGUI recordTimeText;

    private KinectSensor _Sensor;
    private List<SourceManager> _Sources;
    private List<Recorder> _Recorders;
    private bool _recording;
    private string outFolder;
    private float elapsedTime;
    private int numRecordedFrames;
    private System.DateTime startTime;

    #region Monobehaviors
    private void Start() {
        FFmpegLoader.FFmpegPath = Path.Combine(Application.streamingAssetsPath, "ffmpeg-5.1-full_build-shared", "bin");
        _Sensor = KinectSensor.GetDefault();
        _Sources = new List<SourceManager>();
        _Recorders = new List<Recorder>();

        if (_Sensor != null) {
            if (!_Sensor.IsOpen) {
                _Sensor.Open();
            }

            _Sources.Add(new ColorManager(_Sensor));
            _Sources.Add(new DepthManager(_Sensor));
        }
    }

    private void Update() {
        foreach(var source in _Sources) {
            source.UpdateFrame();
            UpdateTextureDisplay(source);
        }

        if (_recording) {
            elapsedTime += Time.deltaTime;
        }
        var hours = Mathf.Floor(elapsedTime / 3600f);
        var remaining = elapsedTime - hours * 3600f;
        var minutes = Mathf.Floor(remaining / 60f);
        remaining -= (minutes * 60f);
        var seconds = Mathf.Floor(remaining);
        recordTimeText?.SetText($"{hours:00}:{minutes:00}:{seconds:00}");
    }

    private void OnApplicationQuit() {
        _recording = false;
        StopRecording();

        if (_Sensor.IsOpen) {
            _Sensor.Close();
        }
        _Sensor = null;

        foreach (SourceManager source in _Sources) {
            source.Dispose();
        }
        _Sources.Clear();
    }
    #endregion

    public void StartRecording() {
        _recording = true;
        startTime = System.DateTime.Now;
        outFolder = Path.Combine(Application.persistentDataPath, "recordings", startTime.ToString("yyyy-M-dd--HH-mm-ss"));
        Directory.CreateDirectory(outFolder);

        foreach (var source in _Sources) {
            _Recorders.Add(new Recorder(source, FrameRate, outFolder));
        }

        StartCoroutine(I_WriteVideo());
    }
    public void StopRecording() {
        _recording = false;
        foreach(var recorder in _Recorders) {
            recorder.Dispose();
        }
        _Recorders.Clear();
        elapsedTime = 0;

        var endTime = System.DateTime.Now;
        var elapsedSeconds = endTime.Subtract(startTime).TotalSeconds;
        using (var logFile = File.OpenWrite(Path.Combine(outFolder, "recordings.log"))) {
            using (var sr = new StreamWriter(logFile)) {
                sr.WriteLine($"Start Time: {startTime}");
                sr.WriteLine($"End Time: {endTime}");
                sr.WriteLine($"Elapsed Seconds: {elapsedSeconds}");
                sr.WriteLine($"Number of Recorded Frames: {numRecordedFrames}");
                sr.WriteLine($"Effective Framerate: {numRecordedFrames / elapsedSeconds:0.##}");
                sr.WriteLine($"Minimum LUT Depth: {DepthManager.MIN_LUT_DEPTH}");
                sr.WriteLine($"Maximum LUT Depth: {DepthManager.MAX_LUT_DEPTH}");
                sr.WriteLine($"LUT Start Color: {DepthManager.LUT_START_COLOR}");
                sr.WriteLine($"LUT End Color: {DepthManager.LUT_END_COLOR}");
                DepthManager.Generate_LUT_Table(outFolder);
            }
        }
        numRecordedFrames = 0;
    }

    private IEnumerator I_WriteVideo() {
        while (_recording) {
            foreach(var recorder in _Recorders) {
                recorder.WriteFrame();
            }
            numRecordedFrames += 1;
            yield return new WaitForSeconds(1f / (float)FrameRate);
        }
    }

    private void UpdateTextureDisplay(SourceManager source) {
        if (source is ColorManager && _colorImage != null) {
            _colorImage.texture = source._Texture;
        } else if (source is DepthManager && _depthImage != null) {
            _depthImage.texture = source._Texture;
        }
    }

    public void OpenExplorerFolder() {
        var itemPath = Path.Combine(Application.persistentDataPath, "recordings");
        Directory.CreateDirectory(itemPath);
        itemPath = itemPath.Replace("/", "\\");
        Debug.Log(itemPath);
        System.Diagnostics.Process.Start("explorer.exe", $"/select,{itemPath}");
    }

}
