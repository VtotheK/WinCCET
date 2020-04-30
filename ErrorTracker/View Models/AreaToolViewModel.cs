using System;
using System.Diagnostics;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorTracker
{
    public class AreaToolWindowViewModel : INotifyPropertyChanged
    {
        bool sectorSelected = false;
        Rect _sectorRect;
        AnalyzeSector _analyzeSector;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool SectorSelected
        {
            get => sectorSelected;
            set
            {
                sectorSelected = value;
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

        public void StartAnalyze()
        {
            _analyzeSector = new AnalyzeSector(_sectorRect);
            _analyzeSector.Start();
        }
    }
}
