using System;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using Accord.Video.FFMPEG;

namespace ErrorTracker
{
    class ClipRecorder
    {
        VideoCaptureDevice camera;

        Bitmap[] buffer = new Bitmap[400];
        int index = 0;
        int num = 0;
        public ClipRecorder()
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            Debug.WriteLine(devices[0].Name);
            camera = new VideoCaptureDevice(devices[0].MonikerString);
            camera.NewFrame += new NewFrameEventHandler(video_NewFrame);
        }

        public void StartRecording()
        {
            //starts recording the video
            camera.Start();
        }

        public void CutVideoClip(VideoInfoArgs args)
        {
            Task.Run(() => {
                Thread.Sleep(3000);
                camera.SignalToStop();
                SaveVideo();
            });

        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            string filepath = Environment.CurrentDirectory;
            Rectangle rect = new Rectangle(0,0,eventArgs.Frame.Width, eventArgs.Frame.Height);
            if (index < buffer.Length) { buffer[index] = eventArgs.Frame.Clone(rect, PixelFormat.Format32bppPArgb); index++; }
            else { index = 0; buffer[index] = eventArgs.Frame.Clone(rect, PixelFormat.Format32bppPArgb); }

            #region For debugging
            // For debugging 
            //string name = System.IO.Path.Combine(filepath, @"TESTIMAGE" + num++.ToString() + ".png");
            //eventArgs.Frame.Save(name, ImageFormat.Png); 
            //if (stopwatch.Elapsed.Seconds > 3)
            //{
            //    SaveVideo();
            //    camera.SignalToStop();
            //}
            // For debugging 
            #endregion
        }

        private void SaveVideo()
        {
            int height = buffer[0].Height;
            int width = buffer[0].Width;
            VideoFileWriter writer = new VideoFileWriter();
            writer.Open("TESTVIDEO" + num++ +".avi", width, height, 15, VideoCodec.MPEG4);
            for(int i = 0 ; i<buffer.Length; i++)
            {
                if (index < buffer.Length)
                {
                    writer.WriteVideoFrame(buffer[index++]);
                }
                else { index = 0; }
            }
            writer.Close();
            camera.Start();
        }
    }
}
