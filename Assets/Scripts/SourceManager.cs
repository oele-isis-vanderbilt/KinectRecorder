using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using System;
using FFMediaToolkit.Graphics;

public abstract class SourceManager : IDisposable {

    protected KinectSensor _Sensor;
    protected IDisposable _FrameReader;
    public FrameDescription _FrameDescription { get; protected set; }
    public byte[] _Data { get; protected set; }
    public Texture2D _Texture { get; protected set; }
    public string Name { get; protected set; }
    public ImagePixelFormat PixelFormat { get; protected set; }

    public void Dispose() {
        if (_FrameReader != null) {
            _FrameReader.Dispose();
            _FrameReader = null;
        }
    }

    public abstract bool UpdateFrame();

}
