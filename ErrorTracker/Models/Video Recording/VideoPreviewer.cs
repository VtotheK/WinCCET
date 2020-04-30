using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using ErrorTracker.BitMapHelpers;

namespace ErrorTracker
{
    public delegate void FrameUpdatedDelegate(BitmapSource src);
    public class VideoPreviewer
    {
        VideoCaptureDevice _device;
        Bitmap _currentFrame;
        BitmapSource btSource;

        public event FrameUpdatedDelegate FrameUpdate;

        public VideoPreviewer(VideoCaptureDevice device)
        {
            Device = device;
        }

        public VideoCaptureDevice Device { get => _device; set => _device = value; }
        public BitmapSource BtSource { get => btSource; set => btSource = value; }

        public void StartStream()
        {
            _device.NewFrame += new NewFrameEventHandler(video_NewFrame);
            _device.Start();
        }

        public void StopStream()
        {
            if (_device != null)
            {
                _device.Stop();
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if(_currentFrame != null)
            {
                _currentFrame.Dispose();
            }
            _currentFrame = (Bitmap)eventArgs.Frame.Clone();
            BtSource = ImageHelper.GetBitmapSourceFromBitmap(_currentFrame);
            BtSource.Freeze();
            FrameUpdate.Invoke(BtSource);
        }
    }
}
