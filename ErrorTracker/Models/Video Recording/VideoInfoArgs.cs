using System;
using System.Threading.Tasks;
namespace ErrorTracker
{
    class VideoInfoArgs : EventArgs
    {
        public DateTime _time { get; set; }
        public string _message { get; set; }
        public Task _currentTask { get; set; }

        public VideoInfoArgs(DateTime time, string message, Task currentTask)
        {
            _time = time;
            _message = message;
            _currentTask = currentTask;
        }
    }
}
