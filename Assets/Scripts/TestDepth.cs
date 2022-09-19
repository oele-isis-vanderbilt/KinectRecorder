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
using System.Runtime.InteropServices;

public class TestDepth : MonoBehaviour {

    public RawImage colorDisplayImage;

    private const int MapDepthToByte = 8000 / 256;
    private KinectSensor kinectSensor;
    private DepthFrameReader depthFrameReader;
    private FrameDescription depthFrameDescription;
    private byte[] depthPixels;
    private Texture2D _Texture;

    private void Start() {
        kinectSensor = KinectSensor.GetDefault();
        depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
        depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
        depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

        _Texture = new Texture2D(depthFrameDescription.Width, depthFrameDescription.Height, TextureFormat.R8, false);

        if (!kinectSensor.IsOpen) {
            kinectSensor.Open();
        }
    }


    private void Update() {
        bool depthFrameProcessed = false;

        using (DepthFrame depthFrame = depthFrameReader.AcquireLatestFrame()) {
            if(depthFrame != null) {
                using(KinectBuffer depthBuffer = depthFrame.LockImageBuffer()){
                    if((depthFrameDescription.Width * depthFrameDescription.Height == depthBuffer.Length / depthFrameDescription.BytesPerPixel)) {
                        //ushort maxDepth = ushort.MaxValue;
                        ushort maxDepth = depthFrame.DepthMaxReliableDistance;
                        ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Length, depthFrame.DepthMinReliableDistance, maxDepth);
                        depthFrameProcessed = true;
                    }
                }
            }
        }

        if (depthFrameProcessed) {
            _Texture.LoadRawTextureData(depthPixels);
            _Texture.Apply();
            colorDisplayImage.texture = _Texture;
        }
    }

    private void OnApplicationQuit() {
        if(depthFrameReader != null) {
            depthFrameReader.Dispose();
            depthFrameReader = null;
        }

        if(this.kinectSensor != null) {
            kinectSensor.Close();
            kinectSensor = null;
        }
    }

    private unsafe void ProcessDepthFrameData(System.IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth) {
        ushort* frameData = (ushort*)depthFrameData;
        int frameDataLength = (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel);

        for(int i=0; i< frameDataLength; ++i) {
            ushort depth = frameData[i];
            this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth/ MapDepthToByte) : 0);
        }
    }
}
