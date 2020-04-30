namespace ErrorTracker
{
    static class ScreenInfo
    {
        public static double Width { get => System.Windows.SystemParameters.PrimaryScreenWidth; }
        public static double Height { get => System.Windows.SystemParameters.PrimaryScreenHeight; }
    }
}
