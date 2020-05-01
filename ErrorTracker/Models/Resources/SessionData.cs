using AForge.Video.DirectShow;

namespace ErrorTracker
{
    public static class SessionData
    {
        public static VideoCaptureDevice UserDevice = null;
        public static int? VideoCapabilityIndex = null;
        public static int? UserFramesPerSecond = null;
        public static string DestinationPath = null;
    }
}
