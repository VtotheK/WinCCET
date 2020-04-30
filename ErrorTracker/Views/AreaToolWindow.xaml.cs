using System;
using System.Windows;

namespace ErrorTracker
{

    /// <summary>
    /// Interaction logic for AreaToolWindow.xaml
    /// </summary>
    public partial class AreaToolWindow : Window
    {
        SectorSelectingTool _secSelTool;
        AreaToolWindowViewModel _viewModel;

        public AreaToolWindow()
        {
            InitializeComponent();
            _viewModel = new AreaToolWindowViewModel();
            _secSelTool = new SectorSelectingTool(_viewModel);
            DataContext = _viewModel;
        }

        private void AreaToolClose_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.AnalyzeIsRunning)
            {
                string message = "Videoseuranta on päällä. Haluatko silti sulkea ikkunan?";
                MessageBoxResult result = MessageBox.Show(message, "Varoitus", MessageBoxButton.YesNo);
                if(result == MessageBoxResult.Yes)
                {
                    _viewModel.StopAllAction();
                    if (_secSelTool.IsLoaded)
                    {
                        _secSelTool.Close();
                    }
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void AreaToolSelectSector_Click(object sender, RoutedEventArgs e)
        {
            if (_secSelTool.IsLoaded)
            {
                _secSelTool.Show();
            }
            else
            {
                _secSelTool = new SectorSelectingTool(_viewModel);
                _secSelTool.Show();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

        private void BeginAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SectorSelected && _viewModel.SectorRect != Rect.Empty && !_viewModel.AnalyzeIsRunning)
            {
                _viewModel.AnalyzeTrigger();
                this.WindowStyle = WindowStyle.None;
            }
            else if (_viewModel.AnalyzeIsRunning)
            {
                _viewModel.AnalyzeTrigger();
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }
    }
}
