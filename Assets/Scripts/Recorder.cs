using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FFMediaToolkit.Encoding;
using FFMediaToolkit;
using FFMediaToolkit.Graphics;

public class Recorder : System.IDisposable {

    private VideoEncoderSettings _Settings;
    private SourceManager _Source;
    private int frameRate;
    private MediaOutput file;

    public Recorder(SourceManager source, int frameRate, string outDirectory) {
        _Source = source;
        this.frameRate = frameRate;
        InitFF();

        Directory.CreateDirectory(outDirectory);
        string outPath = Path.Combine(outDirectory, $"{_Source.Name}.mp4");
        file = MediaBuilder.CreateContainer(outPath).WithVideo(_Settings).Create();
    }

    public void Dispose() {
        file?.Dispose();
    }

    private void InitFF() {
        _Settings = new VideoEncoderSettings(_Source._FrameDescription.Width, _Source._FrameDescription.Height, frameRate, VideoCodec.H264);
        _Settings.EncoderPreset = EncoderPreset.Fast;
        _Settings.CRF = 17;
    }


    public void WriteFrame() {
        ImageData data = new ImageData(new System.Span<byte>(_Source._Data), _Source.PixelFormat, _Source._FrameDescription.Width, _Source._FrameDescription.Height);
        file.Video.AddFrame(data);
    }

}
