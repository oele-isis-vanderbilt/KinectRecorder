using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Common;
using FFMediaToolkit.Graphics;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Audio;
using FFMediaToolkit;
using Windows.Kinect;
using System.Runtime.InteropServices;

public class DepthManager : SourceManager {

    private const int MapDepthToByte = 8000 / 256;
    private static Gradient depthLUT;
    public static readonly int MIN_LUT_DEPTH = 0;
    public static readonly int MAX_LUT_DEPTH = 6000;
    public static readonly Color LUT_START_COLOR = Color.red;
    public static readonly Color LUT_END_COLOR = Color.blue;

    public static void Generate_LUT_Table(string basePath) {
        if (depthLUT == null)
            ConstructLUT();

        using (var LUTStream = File.OpenWrite(Path.Combine(basePath, "LUT.csv"))) {
            using (var LUT_File = new StreamWriter(LUTStream)) {
                for (float i = MIN_LUT_DEPTH; i < MAX_LUT_DEPTH; i++) {
                    var color = depthLUT.Evaluate(i / MAX_LUT_DEPTH);
                    LUT_File.WriteLine($"{i},{color.r},{color.g},{color.b},{color.a}");
                }
            }
        }
    }

    public DepthManager(KinectSensor kinectSensor) {
        this._Sensor = kinectSensor;
        _FrameReader = this._Sensor.DepthFrameSource.OpenReader();
        _FrameDescription = this._Sensor.DepthFrameSource.FrameDescription;
        _Data = new byte[_FrameDescription.Width * _FrameDescription.Height * 4];
        _Texture = new Texture2D(_FrameDescription.Width, _FrameDescription.Height, TextureFormat.RGBA32, false);
        Name = "DepthStream";
        PixelFormat = ImagePixelFormat.Rgba32;
        ConstructLUT();
    }
    
    private static void ConstructLUT() {
        depthLUT = new Gradient();

        GradientColorKey[] colorKey = new GradientColorKey[2];
        colorKey[0].color = LUT_START_COLOR;
        colorKey[0].time = 0.0f;
        colorKey[1].color = LUT_END_COLOR;
        colorKey[1].time = 1.0f;

        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        depthLUT.SetKeys(colorKey, alphaKey);
    }

    public override bool UpdateFrame() {
        bool depthFrameProcessed = false;

        using (DepthFrame depthFrame = ((DepthFrameReader)_FrameReader).AcquireLatestFrame()) {
            if (depthFrame != null) {
                using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer()) {
                    if ((_FrameDescription.Width * _FrameDescription.Height == depthBuffer.Length / _FrameDescription.BytesPerPixel)) {
                        //ushort maxDepth = ushort.MaxValue;
                        ushort maxDepth = depthFrame.DepthMaxReliableDistance;
                        ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Length, depthFrame.DepthMinReliableDistance, maxDepth);
                        depthFrameProcessed = true;
                    }
                }
            }
        }
        if (depthFrameProcessed) {
            _Texture.LoadRawTextureData(_Data);
            _Texture.Apply();
        }

        return depthFrameProcessed;
    }

    private unsafe void ProcessDepthFrameData(System.IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth) {
        ushort* frameData = (ushort*)depthFrameData;
        int frameDataLength = (int)(depthFrameDataSize / _FrameDescription.BytesPerPixel);

        for (int i = 0; i < frameDataLength; ++i) {
            ushort depth = frameData[i];
            Color color = depth >= MIN_LUT_DEPTH && depth <= MAX_LUT_DEPTH ? depthLUT.Evaluate(System.Convert.ToSingle(depth) / (float)MAX_LUT_DEPTH) : Color.black;

            _Data[i * 4 + 0] = Color_FloatToByte(color.r);
            _Data[i * 4 + 1] = Color_FloatToByte(color.g);
            _Data[i * 4 + 2] = Color_FloatToByte(color.b);
            _Data[i * 4 + 3] = Color_FloatToByte(color.a);
        }
    }

    private byte Color_FloatToByte(float color) {
        return (byte)((int)(color * 255));
    }

}
