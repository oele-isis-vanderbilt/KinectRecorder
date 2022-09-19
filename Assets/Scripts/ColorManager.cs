using UnityEngine;
using Windows.Kinect;

public class ColorManager : SourceManager {

    public ColorManager(KinectSensor kinectSensor) {
        _Sensor = kinectSensor;
        _FrameReader = _Sensor.ColorFrameSource.OpenReader();
        _FrameDescription = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        _Data = new byte[_FrameDescription.BytesPerPixel * _FrameDescription.LengthInPixels];
        _Texture = new Texture2D(_FrameDescription.Width, _FrameDescription.Height, TextureFormat.RGBA32, false);
        Name = "ColorStream";
        PixelFormat = FFMediaToolkit.Graphics.ImagePixelFormat.Rgba32;
    }

    public override bool UpdateFrame() {
        bool _FrameProcessed = false;

        using (ColorFrame frame = ((ColorFrameReader)_FrameReader).AcquireLatestFrame()) {
            if(frame != null) {
                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                _FrameProcessed = true;
            }
        }
        if (_FrameProcessed) {
            _Texture.LoadRawTextureData(_Data);
            _Texture.Apply();
        }

        return _FrameProcessed;
    }

}
