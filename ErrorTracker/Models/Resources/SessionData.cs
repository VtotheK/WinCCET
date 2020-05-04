using AForge.Video.DirectShow;
using System.Drawing.Imaging;
namespace ErrorTracker
{
    public static class SessionData
    {
        public static VideoCaptureDevice UserDevice = null;
        public static int? VideoCapabilityIndex = null;
        public static int? UserFramesPerSecond = null;
        public static int? VideoClipLength = null;
        public static int? AfterErrorClipLength = null;
        public static string DestinationPath = null;
    }
}
