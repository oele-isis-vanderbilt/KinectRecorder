using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Common;
using FFMediaToolkit.Graphics;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Audio;
using FFMediaToolkit;
using Windows.Kinect;

public class TestFFMpeg : MonoBehaviour {

    public int frameRate = 30;
    public Gradient depthLUT;
    public RawImage colorDisplayImage;


    private KinectSensor _Sensor;
    private VideoEncoderSettings _settings;
    private bool _recording;

    #region Color Data
    private ColorFrameReader _ColorReader;
    private Texture2D _ColorTexture;
    private byte[] _ColorData;
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }
    #endregion

    #region Depth Data
    private DepthFrameReader _DepthReader;
    private ushort[] _DepthData;
    private Texture2D _DepthTexture;
    public int DepthWidth { get; private set; }
    public int DepthHeight { get; private set; }
    #endregion

    private void Start() {
        InitKinect();
        InitFF();
        ReadFrame();
    }

    private void Update() {
        ReadFrame();
        //colorDisplayImage.texture = _ColorTexture;
        colorDisplayImage.texture = _DepthTexture;
    }

    private void OnApplicationQuit() {
        if (_ColorReader != null) {
            _ColorReader.Dispose();
            _ColorReader = null;
        }

        if (_Sensor != null) {
            if (_Sensor.IsOpen) {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }

    public void StartRecording() {
        _recording = true;
        StartCoroutine(I_WriteVideo());
    }
    public void StopRecording() {
        _recording = false;
    }

    private void ReadFrame() {
        if (_ColorReader != null) {
            var frame = _ColorReader.AcquireLatestFrame();
            var depthFrame = _DepthReader.AcquireLatestFrame();

            if (frame != null) {
                frame.CopyConvertedFrameDataToArray(_ColorData, ColorImageFormat.Rgba);
                _ColorTexture.LoadRawTextureData(_ColorData);
                _ColorTexture.Apply();

                frame.Dispose();
                frame = null;
            }
            
            if(depthFrame != null) {
                depthFrame.CopyFrameDataToArray(_DepthData);
                depthFrame.Dispose();
                depthFrame = null;

                int index = 0;
                foreach(ushort rawValue in _DepthData) {
                    int row = index / DepthWidth;
                    int column = index % DepthHeight;
                    index++;

                    float t = System.Convert.ToSingle(rawValue) / 8000f;
                    _DepthTexture.SetPixel(row, column, depthLUT.Evaluate(t));
                }
                _DepthTexture.Apply();
                //Debug.Log($"Max Value: {_DepthData.Max()}");
                //Debug.Log($"Min Value: {_DepthData.Min()}");
            }

        }
    }

    private IEnumerator I_WriteVideo() {
        string outFolder = Path.Combine(Application.streamingAssetsPath, "recordings");
        Directory.CreateDirectory(outFolder);
        string outPath = Path.Combine(outFolder, System.DateTime.Now.ToString("yyyy-M-dd--HH-mm-ss") + ".mp4");

        using(var file = MediaBuilder.CreateContainer(outPath).WithVideo(_settings).Create()) {
            while (_recording) {
                ImageData data = new ImageData(new System.Span<byte>(_ColorData), ImagePixelFormat.Rgba32, ColorWidth, ColorHeight);
                file.Video.AddFrame(data);
                yield return new WaitForSecondsRealtime(1f / frameRate);
            }
        }
    }

    #region Initialization
    private void InitKinect() {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null) {
            _ColorReader = _Sensor.ColorFrameSource.OpenReader();
            _DepthReader = _Sensor.DepthFrameSource.OpenReader();

            var colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = colorFrameDesc.Width;
            ColorHeight = colorFrameDesc.Height;
            _ColorTexture = new Texture2D(colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
            _ColorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];

            var depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            DepthWidth = depthFrameDesc.Width;
            DepthHeight = depthFrameDesc.Height;
            _DepthTexture = new Texture2D(DepthWidth, DepthHeight, TextureFormat.RGBA32, false);
            _DepthData = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];

            if (!_Sensor.IsOpen) {
                _Sensor.Open();
            }
        }
    }
    private void InitFF() {
        FFmpegLoader.FFmpegPath = Path.Combine(Application.streamingAssetsPath, "ffmpeg-5.1-full_build-shared", "bin");
        _settings = new VideoEncoderSettings(ColorWidth, ColorHeight, frameRate, VideoCodec.H264);
        _settings.EncoderPreset = EncoderPreset.Fast;
        _settings.CRF = 17;
    }
    #endregion

}
