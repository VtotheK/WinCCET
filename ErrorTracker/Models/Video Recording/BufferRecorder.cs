using System;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;
using Accord.Video.FFMPEG;

public delegate void VideoSaved();

namespace ErrorTracker
{
    class BufferRecorder : IDisposable
    {
        public event VideoSaved videoSavedEvent;

        VideoCaptureDevice camera;
        VideoFileWriter writer = new VideoFileWriter();
        Frame[] buffer = new Frame[1200];

        int _frameCount = 0;
        int _framesPerSecond;
        readonly long _fpsChokeMilliseconds;
        Stopwatch _frameCountStopWatch = new Stopwatch();
        Stopwatch _fpsStopWatch = new Stopwatch();
        int index = 0;
        private bool firstRound = true;

        public BufferRecorder()
        {
            camera = SessionData.UserDevice;
            if (SessionData.VideoCapabilityIndex != null)
            {
                camera.VideoResolution = camera.VideoCapabilities[(int)SessionData.VideoCapabilityIndex];
            }
            camera.NewFrame += new NewFrameEventHandler(video_NewFrame);
            _framesPerSecond = 20;
            _fpsChokeMilliseconds = 25;
        }

        public void StartRecording()
        {
            camera.Start();
            _frameCountStopWatch.Start();
            _fpsStopWatch.Start();
        }

        public void StopRecording()
        {
            camera.NewFrame -= new NewFrameEventHandler(video_NewFrame);
            camera.Stop();
            camera = null;
            writer.Dispose();
            Dispose();
        }

        public Task CutVideoClip(VideoInfoArgs args)
        {
            Task task = Task.Run(() => {
                Thread.Sleep(3000);
                camera.SignalToStop();
                Thread.Sleep(500);
                SaveVideo();
            });
            return task;
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if(_fpsStopWatch.ElapsedMilliseconds<_fpsChokeMilliseconds)
            {
                return;
            }
            _frameCount++;
            if(_frameCount > 100)
            {
                _framesPerSecond = _frameCount / (int)_frameCountStopWatch.Elapsed.TotalSeconds;
                _frameCountStopWatch.Restart();
                _frameCount = 0;
            }
            Rectangle rect = new Rectangle(0,0,eventArgs.Frame.Width, eventArgs.Frame.Height);
            if (index < buffer.Length)
            {
                if(buffer[index] != null)
                {
                    buffer[index].Dispose();
                }
                buffer[index++] =  new Frame(eventArgs.Frame.Clone(rect, PixelFormat.Format16bppRgb565));
            }
            else
            {
                index = 0;
                buffer[index].Dispose();
                buffer[index] = new Frame(eventArgs.Frame.Clone(rect, PixelFormat.Format16bppRgb565));
            }
            _fpsStopWatch.Restart();
        }

        private void SaveVideo()
        {
            string fileName = string.Empty;
            string finalDestination = string.Empty;
            int timeStampIndex = 0;
            int width = 0; int height = 0;
            try
            {
                if(index == 0)
                {
                    timeStampIndex = buffer.Length - 2;
                }
                else
                {
                    timeStampIndex = index -1;
                }
                height = buffer[timeStampIndex].Bitmap.Height;
                width = buffer[timeStampIndex].Bitmap.Width;
                fileName = $@"{buffer[timeStampIndex].TimeStamp}.avi";
                finalDestination = $@"{SessionData.DestinationPath}\{fileName}";
            }
            catch(IndexOutOfRangeException e)
            {
                fileName = $@"!{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}.avi";
                finalDestination = $@"{SessionData.DestinationPath}\{fileName}";
                DebugLogger.Log(LogType.Warning,$"Bad timestampindex {DateTime.Now.ToShortTimeString()} in BufferRecorder.cs line 85. Info: Timestampindex: {timeStampIndex} bufferindex: {index} additional info: {e.Message}");

            }
            catch(System.ArgumentException e)
            {
                DebugLogger.Log(LogType.Error, $"ArgumentException @ {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}. More info Paramname:{e.ParamName} message:{e.Message}. No video saved More info: index{index} timestampindex {timeStampIndex} width:{width} height{height}");
                Dispose();
                camera.Start();
                return;
            }
            catch (Exception e)
            {
                DebugLogger.Log(LogType.Warning,e.ToString() + " @BufferRecorder.SaveVideo");
            }
            writer.Open(fileName, width, height, _framesPerSecond, VideoCodec.MPEG4, 4000000); //Bitrate in bytes
            try
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (index < buffer.Length)
                    {
                        if (buffer[index] == null && firstRound)
                        {
                            index = 0;
                            firstRound = false;
                        }
                        else if (buffer[index] == null && !firstRound)
                        {
                            break;
                        }
                        try
                        {
                            writer.WriteVideoFrame(buffer[index++].Bitmap);
                        }
                        catch(System.ArgumentException e)
                        {
                            if (index < buffer.Length - 1)
                            {
                                index++;
                            }
                            else
                            {
                                index = 0;
                            }
                            DebugLogger.Log(LogType.Error, $"ArgumentException. Forced to skip a frame in BufferRecorder.cs line 141. More info: Frame index:{index}, timestampindex: {timeStampIndex} Frame datetime: {buffer[index].TimeStamp}, additional info : {e.Message}");
                        }
                    }
                    else { index = 0; }
                }

            }
            catch(System.ArgumentException e)
            {
                DebugLogger.Log(LogType.Error, $"At bufferRecorder.cs line 135 exception {e.ToString()} occured. Error saving video. More info: Paramname:\"{e.ParamName}\" Message: {e.Message} More info: index{index} timestampindex {timeStampIndex} width:{width} height{height}");
            }
            finally
            {
                firstRound = true;
                writer.Close();
                videoSavedEvent.Invoke();
            }
            try
            {
                if (!File.Exists(finalDestination) && File.Exists(fileName))
                {
                    File.Move(fileName, finalDestination);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                DebugLogger.Log(LogType.Error,$"{ex.ToString()} {ex.Message} Target directiory not found @ BufferRecorder.cs line 122.");
            }
            catch (IOException e) when (File.Exists(finalDestination))
            {
                DebugLogger.Log(LogType.Error, $"At {DateTime.Now.ToShortTimeString()} in BufferRecorder.cs line " +
                    $"127, could not move file from build folder to destinationfolder.");
            }
            catch(IOException e) when (!File.Exists(fileName))
            {
                DebugLogger.Log(LogType.Error, $"At {DateTime.Now.ToShortTimeString()} in BufferRecorder.cs line " +
                    $"132, could not locate the video file in build folder.");

            }
            Dispose();
            index = 0;
            _frameCountStopWatch.Restart();
            _frameCount = 0;
            camera.Start();
        }

        public void Dispose()
        {
            foreach(Frame frame in buffer)
            {
                if (frame != null)
                {
                    frame.Dispose();
                }
            }
        }

        private class Frame : IDisposable
        {
            Bitmap _bitmap;
            private DateTime timeStamp;

            public Frame(Bitmap bitmap)
            {
                _bitmap = bitmap;
                timeStamp = DateTime.Now;
            }

            public Bitmap Bitmap
            {
                get => _bitmap;
            }

            public string TimeStamp
            {
                get
                {
                    return timeStamp.ToString("dd.MM.yyyy HH:mm:ss");
                }
            }

            public void Dispose()
            {
                Bitmap.Dispose();
            }
        }
    }
}

