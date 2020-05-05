using System;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using AForge.Video.DirectShow;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace ErrorTracker
{
    sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        MainWindow _mainWindow;
        const int FolderDestinationMaxChars = 50;
        const int Bits = 16;
        const string NoFolderDestination = "Ei polkua";

        BitmapSource previewSource;
        RecorderCollection recorderCollection = new RecorderCollection();

        List<string> _availableResolutions = new List<string>();
        List<string> _availableFramesPerSecond = new List<string>();
        List<string> _recorderNames;
        string _folderDialogDestination = NoFolderDestination;
        string _RAMUsageApproximation;
        string _clipLengthApproximation;
        bool _isSelectionsMade = false;

        CommonFileDialogResult _folderResult = CommonFileDialogResult.None;


        VideoCaptureDevice _userDevice = null;
        VideoPreviewer vPreview = new VideoPreviewer(null);
        VideoCaptureDevice selectedDevice = new VideoCaptureDevice();
        public MainWindowViewModel(MainWindow main)
        {
            _mainWindow = main;
            RefreshRecorderList();
        }

        
        #region Fields
        public BitmapSource PreviewSource
        {
            get
            {
                return previewSource;
            }
            set
            {
                previewSource = value;
                OnPropertyChanged(nameof(MainWindowViewModel.previewSource));
            }
        }

        public List<string> RecorderNames
        {
            get
            {
                return _recorderNames;
            }
            private set
            {
                _recorderNames = value;
                OnPropertyChanged(nameof(MainWindowViewModel.RecorderNames));
            }
        }

        public string FolderDialogDestination
        {
            get
            {
                return _folderDialogDestination;
            }
            private set
            {
                SessionData.DestinationPath = value;
                if (value.Length > FolderDestinationMaxChars)
                {
                    _folderDialogDestination = value.Remove(FolderDestinationMaxChars - 1) + "...";
                }
                else
                {
                    _folderDialogDestination = value;
                }
                OnPropertyChanged(nameof(MainWindowViewModel.FolderDialogDestination));
                CheckSelections();
            }
        }
        public VideoCaptureDevice UserDevice
        {
            get
            {
                return _userDevice;
            }
            set
            {
                SessionData.UserDevice = value;
                _userDevice = value;
                OnPropertyChanged(nameof(MainWindowViewModel.UserDevice));
                CheckSelections();
            }
        }

        public bool IsSelectionsMade
        {
            get
            {
                return _isSelectionsMade;
            }
            private set
            {
                _isSelectionsMade = value;
                OnPropertyChanged(nameof(MainWindowViewModel.IsSelectionsMade));
            }
        }

        public List<string> AvailableResolutions
        {
            get
            {
                return _availableResolutions;
            }
            set
            {
                _availableResolutions = value;
                OnPropertyChanged(nameof(MainWindowViewModel.AvailableResolutions));
            }
        }

        public List<string> AvailableFramesPerSecond
        {
            get
            {
                return _availableFramesPerSecond;
            }
            set
            {
                _availableFramesPerSecond = value;
                OnPropertyChanged(nameof(MainWindowViewModel.AvailableFramesPerSecond));
            }
        }

        public string ClipLengthApproximation
        {
            get
            { 
                return _clipLengthApproximation;
            }
            set
            {
                _clipLengthApproximation = value;
                OnPropertyChanged(nameof(MainWindowViewModel.ClipLengthApproximation));
            }
        }
        public string RAMUsageApproximation
        {
            get
            { 
                return _RAMUsageApproximation;
            }
            set
            {
                _RAMUsageApproximation = value;
                OnPropertyChanged(nameof(MainWindowViewModel.RAMUsageApproximation));
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public void RefreshRecorderList()
        {
            RecorderNames = recorderCollection.RefreshRecorderNames();
        }

        public void ShowPreviewStream(string itemName)
        {
            if (itemName != null)
            {
                if (selectedDevice != null)
                {
                    selectedDevice = null;
                }
                if(vPreview != null)
                {
                    vPreview.StopStream();
                    vPreview.FrameUpdate -= RecieveFrame;
                    vPreview = new VideoPreviewer(null);
                }
                selectedDevice = SearchVideoRecorders(itemName);
                if(selectedDevice == null) 
                    return;
                vPreview.Device = selectedDevice;
                vPreview.FrameUpdate += RecieveFrame;
                vPreview.StartStream();
            }
        }

        public void SetUserRecorder(string itemName)
        {
            UserDevice = SearchVideoRecorders(itemName);
            ShowAvailableResolutions(UserDevice);
            ShowAvailableFramesPerSecond(UserDevice);
            CalculateRAMUsageAndClipLength();
        }

        public void CalculateRAMUsageAndClipLength()
        {
            if (SessionData.VideoClipLength != null && SessionData.AfterErrorClipLength != null)
            {
                int clipLength = (int)SessionData.VideoClipLength;
                int afterErrorLength = (int)SessionData.AfterErrorClipLength;
                _mainWindow.ClipLengthApproximation.Text = (clipLength+afterErrorLength).ToString() + 's';
                if (SessionData.UserDevice != null && SessionData.UserFramesPerSecond != null)
                {
                    int frameX = SessionData.UserDevice.VideoCapabilities[(int)SessionData.VideoCapabilityIndex].FrameSize.Width;
                    int frameY = SessionData.UserDevice.VideoCapabilities[(int)SessionData.VideoCapabilityIndex].FrameSize.Height;
                    float RAMUsage = (((float)frameX * frameY * 16 / 8) / 1000000 * (clipLength + afterErrorLength) * (int)SessionData.UserFramesPerSecond);
                    _mainWindow.RAMUsageApproximation.Text = ((int)RAMUsage).ToString() + "Mt";
                    if (RAMUsage > 3000)
                        _mainWindow.RAMUsageApproximation.Background = Brushes.Red;
                    else if (RAMUsage > 2500)
                        _mainWindow.RAMUsageApproximation.Background = Brushes.Yellow;
                    else
                        _mainWindow.RAMUsageApproximation.Background = Brushes.LightGray;
                }
            }
        }

        private void ShowAvailableFramesPerSecond(VideoCaptureDevice device)
        {
            List<string> fps = new List<string>();
            int frames = device.VideoCapabilities[0].MaximumFrameRate;
            frames -= frames % 10;
            fps.Add(frames.ToString() + "fps");
            while (frames > 5)
            {
                frames -= 5;
                fps.Add(frames.ToString() + "fps");
            }
            AvailableFramesPerSecond = fps;
            _mainWindow.FPSComboBox.SelectedIndex = 0;
            SessionData.UserFramesPerSecond = Convert.ToInt32(fps[0].Trim('f','p','s'));
        }

        private VideoCaptureDevice SearchVideoRecorders(string itemName)
        {
            foreach (FilterInfo camInfo in recorderCollection.Recorders)
            {
                if (itemName == camInfo.Name)
                    return new VideoCaptureDevice(camInfo.MonikerString);
            }
            return null;
        }

        public void ShowAvailableResolutions(VideoCaptureDevice device)
        {
            List<string> resolutions = new List<string>();
            foreach (VideoCapabilities cap in device.VideoCapabilities)
            {
                bool width = false;
                string[] arr = Regex.Split(cap.FrameSize.ToString(), @"\D+");
                string res = string.Empty;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (String.IsNullOrEmpty(arr[i]))
                        continue;
                    res += arr[i];
                    if (!width)
                    {
                        width = true;
                        res += 'x';
                    }
                }
                resolutions.Add(res);
            }
            AvailableResolutions = resolutions;
            _mainWindow.ResolutionComboBox.SelectedIndex = 0;
            SessionData.VideoCapabilityIndex = _mainWindow.ResolutionComboBox.SelectedIndex;
        }

        private void RecieveFrame(BitmapSource source)
        {
            PreviewSource = source;
        }

        public void StopRecording()
        {
            vPreview.Device.Stop();
            vPreview.BtSource = null;
            previewSource = null;
        }
        

        public void OpenFolderDialog()
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            _folderResult = folderDialog.ShowDialog();
            if (_folderResult == CommonFileDialogResult.Ok)
            {
                if (Directory.Exists(folderDialog.FileName))
                {
                    FolderDialogDestination = folderDialog.FileName;
                }
            }
            else
            {
                FolderDialogDestination = NoFolderDestination;
                CheckSelections();
            }
        }

        private void CheckSelections()
        {
            if (_folderResult != CommonFileDialogResult.Ok || _userDevice == null)
            {
                IsSelectionsMade = false;
            }
            else
            {
                IsSelectionsMade = true;
            }
        }
    }
}
