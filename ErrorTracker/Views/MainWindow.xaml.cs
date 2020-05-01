using System;
using System.Diagnostics;
using System.Windows;

namespace ErrorTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;
        AreaToolWindow areaToolWindow;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainWindowViewModel(this);
            DataContext = viewModel;
        }

        private void AreaTool_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.IsSelectionsMade)
            {
                SessionData.UserDevice = viewModel.UserDevice;
                DebugLogger.CreateLog(SessionData.DestinationPath);
                if(!DebugLogger.LogPathExists())
                {
                    MessageBoxButton button = MessageBoxButton.OK;
                    string message = "Ei määritettyä polkua bugi-lokin kirjoittamiselle löytynyt. Jatketaan ilman lokitusta.";
                    MessageBox.Show(message,"Varoitus", button);
                }
                WindowState = WindowState.Minimized;
                viewModel.StopRecording();
                areaToolWindow = new AreaToolWindow();
                areaToolWindow.Show();
            }
            
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RefreshRecorderList();
        }

        private void VideoSelectionCMB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(VideoSelectionCMB.SelectedItem != null)
            {
                viewModel.ShowPreviewStream(VideoSelectionCMB.SelectedItem.ToString());
                viewModel.SetUserRecorder(VideoSelectionCMB.SelectedItem.ToString());
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenFolderDialog();
        }

        private void ResolutionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SessionData.VideoCapabilityIndex = ResolutionComboBox.SelectedIndex;
        }

        private void FPSComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int result;
            if (Int32.TryParse(viewModel.AvailableFramesPerSecond[FPSComboBox.SelectedIndex].Trim('f', 'p', 's'), out result))
                SessionData.UserFramesPerSecond = result;
        }
    }
}
