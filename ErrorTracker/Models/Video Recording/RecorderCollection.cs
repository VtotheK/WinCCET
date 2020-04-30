using System.Collections.Generic;
using AForge.Video.DirectShow;

namespace ErrorTracker
{
    class RecorderCollection
    {
        private FilterInfoCollection _recorders;
        private List<string> _recorderNames;

        public RecorderCollection()
        {
            _recorders = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _recorderNames = new List<string>();
        }

        public FilterInfoCollection Recorders { get => _recorders; }
        public List<string> RecorderNames { get => RefreshRecorderNames();}

        public List<string> RefreshRecorderNames()
        {
            _recorderNames.Clear();
            _recorders = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if(_recorders.Count < 1)
            {
                _recorderNames.Add("No recorders found");
            }
            else
            {
                foreach(FilterInfo item in _recorders)
                {
                    _recorderNames.Add(item.Name);
                }
            }
            return _recorderNames;
        }
    }
}
