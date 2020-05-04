using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using ErrorTracker.BitMapHelpers;
 
namespace ErrorTracker
{
    public enum PixelColor { None, White, Gray, Green, Pink, Red, Other = 11 }
    delegate Task AnalyzeSectorChange(VideoInfoArgs args);
    class AnalyzeSector //TODO implement a proper treshold for breaking out of nested colorInRange loop
    {
        public event AnalyzeSectorChange OnAnalyzeSectorChange;

        private enum ImageState { None = -1, Normal=1, HandDrive=5, InError=100, NotActive=999 }
        readonly ColorRange _redColorRange = new ColorRange(Color.FromArgb(255, 255, 0, 0), 50);
        readonly ColorRange _greenColorRange = new ColorRange(Color.FromArgb(255, 0, 255, 0), 50);
        readonly ColorRange _pinkColorRange = new ColorRange(Color.FromArgb(255, 255, 128, 128), 20);
        readonly ColorRange _whiteColorRange = new ColorRange(Color.FromArgb(255, 255, 255, 255), 30);
        readonly ColorRange _grayColorRange = new ColorRange(Color.FromArgb(255, 157, 157, 157), 20);

        readonly int _differenceThreshold;
        
        BufferRecorder _bufferRecorder;
        AreaToolWindowViewModel _viewModel;
        Rect _sectorRect;
        CancellationTokenSource _tokensource = new CancellationTokenSource();
        CancellationToken _cancellationToken;
        Task _recordingTask;

        Random rnd = new Random(); // TODO DEBUG DEBUG DEBUG
        int time = -1;
        Stopwatch sw = new Stopwatch();
        #region Constructors

        public AnalyzeSector(Rect sectorRect , AreaToolWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _sectorRect = sectorRect;
            _cancellationToken = _tokensource.Token;
            _differenceThreshold = 20;
        }
        #endregion
        #region destructor 
        ~AnalyzeSector()
        {
            DebugLogger.Log(LogType.Information, $"AnalyzeSector killed at {DateTime.Now.ToString()}");
        }
        #endregion

        public void Start()
        {
            sw.Start();
            DebugLogger.Log(LogType.Information, $"AnalyzeSector started @ {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            _bufferRecorder = new BufferRecorder();
            _bufferRecorder.videoSavedEvent += ClipSaved;
            OnAnalyzeSectorChange += _bufferRecorder.CutVideoClip;
            Task.Factory.StartNew(() => AnalyzeLoop(_sectorRect), TaskCreationOptions.LongRunning);
            _recordingTask = Task.Factory.StartNew(() => _bufferRecorder.StartRecording(),TaskCreationOptions.LongRunning);
        }

        private void ClipSaved() //Bubble up the value to areatoolwindow 
        {
            _viewModel.ClipsSavedCount++;
        }

        public void Stop()
        {
            _tokensource.Cancel();
            OnAnalyzeSectorChange -= _bufferRecorder.CutVideoClip;
            _bufferRecorder.StopRecording();
        }

        public void AnalyzeLoop(Rect sectorRect)
        {
            int line = new StackFrame(1).GetFileLineNumber();
            if (sectorRect == null)
            {
                DebugLogger.Log(LogType.FATALERROR,$"SectorRect was null @ AnalyzeSector line 80");
                return;
            }
            time = rnd.Next(100000, 120000); //TODO debug debug debug REMOVE

            Thread.Sleep(1000); //Filling the buffer
            ImageData previousFrameImData = new ImageData();
            ImageData currentFrameImData = new ImageData();
            Bitmap previousFrame = ImageHelper.GetBitmapFromSector(sectorRect);

            try
            {

                while (true && !_cancellationToken.IsCancellationRequested)
                {

                    Bitmap currentFrame = ImageHelper.GetBitmapFromSector(sectorRect);
                    if (currentFrame == null)
                    {
                        DebugLogger.Log(LogType.Warning,"CurrentFrame is null @ AnalyzeSector.cs line 84. Could not get bitmap from sector from SectorFromScreenShot.GetBitmapFromSector");
                        continue;
                    }
                    if (previousFrame == null)
                    {
                        DebugLogger.Log(LogType.Warning,"PreviousFrame is null @ AnalyzeSector.cs line 84.");
                        previousFrame = currentFrame;
                    }
                    
                        for (int i = 0; i < currentFrame.Width; i++) 
                        {
                            for (int j = 0; j < currentFrame.Height; j++)
                            {
                                Color sample = currentFrame.GetPixel(i, j);
                                PixelColor pixelColor = PixelType(sample);

                                switch (pixelColor)
                                {
                                    case PixelColor.Red:
                                        currentFrameImData.Red++;
                                        break;

                                    case PixelColor.Green:
                                        currentFrameImData.Green++;
                                        break;

                                    case PixelColor.Pink:
                                        currentFrameImData.Pink++;
                                        break;

                                    case PixelColor.White:
                                        currentFrameImData.White++;
                                        break;

                                    case PixelColor.Gray:
                                        currentFrameImData.Gray++;
                                        break;
                                }
                            }
                        }
                    Debug.WriteLine("------------------------------------------------------------");
                    Debug.WriteLine($"Detected pixels: Red: {currentFrameImData.Red}, Green: {currentFrameImData.Green}, Pink: {currentFrameImData.Pink}, White{currentFrameImData.White}, Gray {currentFrameImData.Gray}");
                    Debug.WriteLine($"{previousFrameImData.ImageState.ToString()}");
                    TrackChanges(previousFrameImData, currentFrameImData);
                    //if (sw.IsRunning)
                    //{
                    //    if (sw.Elapsed.Seconds >= 15) { sw.Stop(); sw.Reset(); Debug.WriteLine("Stopped SW!"); }
                    //}
                    //else
                    //{
                    //    if (previousFrameImData.ImageState != ImageState.None)
                    //    {
                    //        if (previousFrameImData.ImageState == ImageState.Normal)
                    //        {
                    //            if (currentFrameImData.ImageState == ImageState.InError) //Green -> Red state change
                    //            {
                    //                if (OnAnalyzeSectorChange != null)
                    //                {
                    //                    DebugLogger.Log(LogType.Information, $"{previousFrameImData.ImageState.ToString()} --> {currentFrameImData.ImageState.ToString()}" +
                    //                        $" state change detected. OnAnalyzeSectorChange invoked at {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
                    //                    VideoInfoArgs vInfoArgs = new VideoInfoArgs(DateTime.Now, "Normal->InError state change.");
                    //                    Task task = OnAnalyzeSectorChange(vInfoArgs);
                    //                    task.Wait();
                    //                }
                    //            }
                    //        }
                    //        else if (previousFrameImData.ImageState == ImageState.InError && currentFrameImData.ImageState == ImageState.Normal) //Red->Green state change
                    //        {
                    //            sw.Start();
                    //            Debug.WriteLine("sw started");
                    //        }
                    //        else if (previousFrameImData.ImageState == ImageState.NotActive && currentFrameImData.ImageState == ImageState.InError) //Gray -> Red state change
                    //        {
                    //            if (OnAnalyzeSectorChange != null)
                    //            {
                    //                DebugLogger.Log(LogType.Information, $"{previousFrameImData.ImageState.ToString()} --> {currentFrameImData.ImageState.ToString()}" +
                    //                    $" state change detected. OnAnalyzeSectorChange invoked at {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
                    //                VideoInfoArgs vInfoArgs = new VideoInfoArgs(DateTime.Now, "NotActive -> InError state change at");
                    //                Task task = OnAnalyzeSectorChange(vInfoArgs);
                    //                task.Wait();
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        currentFrameImData.ImageState = ImageState.Normal;
                    //    }
                    //}

                    if(sw.Elapsed.TotalMilliseconds> time)
                    {
                        DebugLogger.Log(LogType.Information, $"{previousFrameImData.ImageState.ToString()} --> {currentFrameImData.ImageState.ToString()}" +
                                        $" state change detected. OnAnalyzeSectorChange invoked at {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
                        VideoInfoArgs vInfoArgs = new VideoInfoArgs(DateTime.Now, "NotActive -> InError state change at", _recordingTask);
                        Task task = OnAnalyzeSectorChange(vInfoArgs);
                        task.Wait();
                        sw.Restart();
                        time = 0;
                        time = rnd.Next(100000, 1200000);
                    }
                    previousFrameImData.Gray = currentFrameImData.Gray;
                    previousFrameImData.Green = currentFrameImData.Green;
                    previousFrameImData.Red = currentFrameImData.Red;
                    previousFrameImData.White= currentFrameImData.White;
                    previousFrameImData.ImageState = currentFrameImData.ImageState;
                    currentFrameImData.ClearData();
                    Thread.Sleep(1000); //Look for changes in the sector every 1000ms
                    Debug.WriteLine($"Random:{time}    sw.elaped ms: {sw.Elapsed.TotalMilliseconds}");
                    Debug.WriteLine("");
                }
            }
            catch (Exception e)
            {
                Stop();
                string message = $"At {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} in AnalyzeSector.cs line " +
                  $"182 exception: {e.ToString()}. Analyze loop was terminated.";
                DebugLogger.Log(LogType.FATALERROR,$"{message}");
                MessageBox.Show(message);
                return;
            }
            return;
        }

        private void TrackChanges(ImageData previousFrameImData, ImageData currentFrameImData)
        {
            bool stateSelected = false; bool multipleStates = false;
            if(Math.Abs(previousFrameImData.Other - currentFrameImData.Other) / (_sectorRect.Height * _sectorRect.Width) * 10 >= _differenceThreshold)
            {
                currentFrameImData.ImageState = previousFrameImData.ImageState;
            }
            int redDifference = Math.Abs(previousFrameImData.Red - currentFrameImData.Red);
            int whiteDifference = Math.Abs(previousFrameImData.White - currentFrameImData.White);
            int greenDifference = Math.Abs(previousFrameImData.Green - currentFrameImData.Green);
            int pinkDifference = Math.Abs(previousFrameImData.Pink - currentFrameImData.Pink);
            //Debug.WriteLine("");
            //Debug.WriteLine("Pixel difference from last frame:");
            //Debug.WriteLine($"{redDifference} red difference");
            //Debug.WriteLine($"{whiteDifference} white difference");
            //Debug.WriteLine($"{greenDifference} green difference");
            //Debug.WriteLine($"{pinkDifference} pink difference");
            //Debug.WriteLine("Sector size: " + _sectorRect.Height * _sectorRect.Width);
            //Debug.WriteLine("--------------------------------------------------------");
            if (currentFrameImData.Red> _sectorRect.Height * _sectorRect.Width / 100 * _differenceThreshold) //TODO What if more than 1 is over threshold
            {
                stateSelected = true;
                currentFrameImData.ImageState = ImageState.InError;
            } 
            if(currentFrameImData.Green> (_sectorRect.Height * _sectorRect.Width / 100) * _differenceThreshold)
            {
                if (!stateSelected)
                {
                    currentFrameImData.ImageState = ImageState.Normal;
                    stateSelected = true;
                }
                else
                {
                    multipleStates = true;
                }
            }
            if (currentFrameImData.Gray > _sectorRect.Height * _sectorRect.Width / 100 * _differenceThreshold)
            {
                if (!stateSelected)
                {
                    stateSelected = true;
                    currentFrameImData.ImageState = ImageState.NotActive;
                }
                else
                {
                    multipleStates = true;
                }
            }
            if (currentFrameImData.Pink > _sectorRect.Height * _sectorRect.Width / 100 * _differenceThreshold)
            {
                if (!stateSelected)
                {
                    stateSelected = true;
                    currentFrameImData.ImageState = ImageState.HandDrive;
                }
                else
                {
                    multipleStates = true;
                }
            }
            if(!stateSelected) { currentFrameImData.ImageState = previousFrameImData.ImageState; }
            if(multipleStates)
            {
                DebugLogger.Log(LogType.Warning, $"{DateTime.Now.ToShortTimeString()} More than one state changes detected @ AnalyzeSector.cs.TrackChanges(). Current video logs may be unreliable! \n Error state prioritized over other states."); //TODO tell user that video logs might be unreliable
            }
        }

        private PixelColor PixelType(Color sample) //Double the channel threshold value for red and green for their Pixelcolor enum
        {
            #region Red color testing 
            if (_redColorRange.ColorValue.R - _redColorRange.MatchThreshold * 2 <= sample.R &&
               _redColorRange.ColorValue.G + _redColorRange.MatchThreshold >= sample.G &&
               _redColorRange.ColorValue.B + _redColorRange.MatchThreshold >= sample.B)
            {
                return PixelColor.Red;
            }
            #endregion
            #region Green color testing
            else if (_greenColorRange.ColorValue.R + _greenColorRange.MatchThreshold >= sample.R &&
                    _greenColorRange.ColorValue.G - _greenColorRange.MatchThreshold * 2 <= sample.G &&
                    _greenColorRange.ColorValue.B + _greenColorRange.MatchThreshold >= sample.B)
            {
                return PixelColor.Green;
            }
            #endregion
            #region Pink color testing
            else if ((_pinkColorRange.ColorValue.R - _pinkColorRange.MatchThreshold * 2 <= sample.R &&
                    _pinkColorRange.ColorValue.B + _pinkColorRange.MatchThreshold >= sample.B &&
                    _pinkColorRange.ColorValue.G + _pinkColorRange.MatchThreshold >= sample.G) &&
                    _pinkColorRange.ColorValue.B - _pinkColorRange.MatchThreshold <= sample.B &&
                    _pinkColorRange.ColorValue.G - _pinkColorRange.MatchThreshold <= sample.G)
            {
                return PixelColor.Pink;
            }
            #endregion
            #region White color testing
            else if (_whiteColorRange.ColorValue.R - _whiteColorRange.MatchThreshold <= sample.R &&
                    _whiteColorRange.ColorValue.B - _whiteColorRange.MatchThreshold <= sample.B &&
                    _whiteColorRange.ColorValue.G - _whiteColorRange.MatchThreshold <= sample.G)
            {
                return PixelColor.White;
            }
            else if(_grayColorRange.ColorValue.R - _grayColorRange.MatchThreshold <= sample.R &&
                    _grayColorRange.ColorValue.B - _grayColorRange.MatchThreshold <= sample.B &&
                    _grayColorRange.ColorValue.G - _grayColorRange.MatchThreshold <= sample.G &&
                    _grayColorRange.ColorValue.R + _grayColorRange.MatchThreshold >= sample.R &&
                    _grayColorRange.ColorValue.B + _grayColorRange.MatchThreshold >= sample.B &&
                    _grayColorRange.ColorValue.G + _grayColorRange.MatchThreshold >= sample.G)
            {
                return PixelColor.Gray;
            }
            #endregion
            else
            {
                return PixelColor.Other;
            }
        }

        private class ImageData
        {
            ImageState _imageState;
            int _red;
            int _green;
            int _pink;
            int _white;
            int _gray;
            int _other;
            public ImageData()
            {
                _red = 0;
                _green = 0;
                _pink = 0;
                _white = 0;
                _imageState = ImageState.None;
            }

            public ImageData(int red, int green, int pink, int white, int gray, int other, ImageState imageState)
            {
                _red = red;
                _green = green;
                _pink = pink;
                _white = white;
                _gray = gray;
                Other = other;
                ImageState = imageState;
            }

            public int Red { get => _red; set => _red = value; }
            public int Green { get => _green; set => _green = value; }
            public int Pink { get => _pink; set => _pink = value; }
            public int White { get => _white; set => _white = value; }
            public int Gray { get => _gray; set => _gray = value; }
            public int Other { get => _other; set => _other = value; }
            public ImageState ImageState { get => _imageState; set => _imageState = value; }

            public void ClearData()
            {
                Red = 0;
                Green = 0;
                Pink = 0;
                White = 0;
                Gray = 0;
                Other = 0;
                ImageState = ImageState.None;
            }
            public ImageData Clone()
            {
                return new ImageData(Red, Green, Pink, White, Gray, Other, ImageState);
            }
        }
    }
}