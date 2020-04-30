using System.Windows;

namespace ErrorTracker
{
    /// <summary>
    /// Interaction logic for ImageDebugWindow.xaml
    /// </summary>
    public partial class ImageDebugWindow : Window
    {
        public ImageDebugWindow()
        {
            InitializeComponent();
        }

        private void Close_Window_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
