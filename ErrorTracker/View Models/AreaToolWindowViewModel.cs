using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.ComponentModel;

namespace ErrorTracker
{
    public class AreaToolWindowViewModel : INotifyPropertyChanged
    {
        const int Margin = 100;

        bool _sectorSelected = false;
        bool _sectorCanBeSelected = true;
        bool _analyzeIsRunning = false;
        Visibility dontTouchTextVisibility = Visibility.Hidden;
        string _analyzeButtonText = "Start tracking";
        string _clipsSavedText = "Saved video clips: 0";
        int _clipsSavedCount = 0;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();
        CancellationToken blinkToken;
        Rect _sectorRect;
        AnalyzeSector _analyzeSector;
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public AreaToolWindowViewModel()
        {
            _clipsSavedCount = 0;
        }
        #region INotifyPropertyChanged properties
        public bool SectorSelected
        {
            get => _sectorSelected;
            set
            {
                _sectorSelected = value;
                OnPropertyChanged(nameof(AreaToolWindowViewModel.SectorSelected));
            }
        }

        public Rect SectorRect
        {
            get => _sectorRect;
            set
            {
                _sectorRect = value;
            }
        }

        public string AnalyzeButtonText
        {
            get => _analyzeButtonText;
            set
            {
                _analyzeButtonText = value;
                OnPropertyChanged(nameof(AreaToolWindowViewModel.AnalyzeButtonText));
            }
        }

        public bool SectorCanBeSelected
        {
            get => _sectorCanBeSelected;
            set
            {
                _sectorCanBeSelected = value;
                OnPropertyChanged(nameof(AreaToolWindowViewModel.SectorCanBeSelected));
            }
        }

        public bool AnalyzeIsRunning
        {
            get => _analyzeIsRunning;
            set
            {
                OnPropertyChanged(nameof(AreaToolWindowViewModel.AnalyzeIsRunning));
                _analyzeIsRunning = value;
            }
        }

        public Visibility DontTouchTextVisibility
        {
            get => dontTouchTextVisibility;

            set
            {
                dontTouchTextVisibility = value;
                OnPropertyChanged(nameof(AreaToolWindowViewModel.DontTouchTextVisibility));
            }
        }

        public string ClipsSavedText
        {
            get => _clipsSavedText;
            set 
            {
                _clipsSavedText = value;
                OnPropertyChanged(nameof(AreaToolWindowViewModel.ClipsSavedText));
            } 
        }

        public int ClipsSavedCount
        {
            get => _clipsSavedCount;
            set
            {
                _clipsSavedCount = value;
                ClipsSavedText = "Tallennettuja videoita: " + _clipsSavedCount.ToString();
            }
        }
        #endregion
        public void AnalyzeTrigger()
        {
            if (SectorSelected && SectorCanBeSelected)
            {
                blinkToken = _tokenSource.Token;
                if(_analyzeSector != null)
                    _analyzeSector = null;
                _analyzeSector = new AnalyzeSector(_sectorRect,this);
                _analyzeSector.Start();
                AnalyzeIsRunning = true;
                SectorCanBeSelected = false;
                AnalyzeButtonText = "End tracking";
                Task.Run(() => StartBlink(blinkToken));
                DontTouchTextVisibility = Visibility.Visible;
            }
            else
            {
                ClipsSavedCount = 0;
                _tokenSource.Cancel();
                _analyzeSector.Stop();
                SectorCanBeSelected = true;
                AnalyzeIsRunning = false;
                AnalyzeButtonText = "Start tracking";
                DontTouchTextVisibility = Visibility.Hidden;
                _analyzeSector = null;
            }
        }

        private void StartBlink(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                if(DontTouchTextVisibility == Visibility.Visible)
                    DontTouchTextVisibility = Visibility.Hidden;
                else
                    DontTouchTextVisibility = Visibility.Visible;
                Thread.Sleep(500);
            }
        }

        public void ClipSavedInc(int val)
        {
            ClipsSavedCount = val;
        }

        public void StopAllAction()
        {
            if(_analyzeSector != null)
            {
                _analyzeSector.Stop();
            }
        }

    }
}
