using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorTracker.BitMapHelpers;

namespace ErrorTracker
{
    public delegate void SectorSelected(bool arg);

    public partial class SectorSelectingTool : Window
    {
        const double BorderOffSet = 20;
        bool mouseDown = false;
        AreaToolWindowViewModel _areaToolWindowViewModel;
        System.Windows.Point _mousePos;
        Rect _sectorRect;

        public event SectorSelected OnSectorSelected; 

        public SectorSelectingTool(AreaToolWindowViewModel areaToolWindowViewModel)
        {
            InitializeComponent();
            Height = ScreenInfo.Height;
            Width = ScreenInfo.Width;
            _areaToolWindowViewModel = areaToolWindowViewModel;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SelectionRect != null && !mouseDown)
            {
                SelectionRect.Visibility = Visibility.Visible;
                SelectionRect.Height = 1;
                SelectionRect.Width = 1;

                _mousePos = Mouse.GetPosition(this);

                Canvas.SetTop(SelectionRect, _mousePos.Y);
                Canvas.SetLeft(SelectionRect, _mousePos.X);
            }
            mouseDown = true;
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown && SelectionRect != null)
            {
                System.Windows.Point currentMousePos = e.GetPosition(this);

                double currentX = currentMousePos.X;
                double currentY = currentMousePos.Y;

                if (_mousePos.X < currentX)
                {
                    SelectionRect.Width = currentX - _mousePos.X;
                }
                else if (_mousePos.X > currentX)
                {
                    Canvas.SetLeft(SelectionRect, currentX);
                    SelectionRect.Width = Math.Abs(currentX - _mousePos.X);
                }

                if (_mousePos.Y < currentY)
                {
                    SelectionRect.Height = currentY - _mousePos.Y;
                }
                else if (_mousePos.Y > currentY)
                {
                    Canvas.SetTop(SelectionRect, currentY);
                    SelectionRect.Height = Math.Abs(currentY - _mousePos.Y);
                }
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
            double left = Canvas.GetLeft(SelectionRect); 
            double top = Canvas.GetTop(SelectionRect) - BorderOffSet; //20 pixel offset from top on window, borderless window is the reason?
             
            double strokeThickness = SelectionRect.StrokeThickness; // To remove the red dash-border from the bitmap
            if (SelectionRect != null && SelectionRect.Height > 5 && SelectionRect.Width > 5)
            {
                _sectorRect = new Rect(left + strokeThickness, top + strokeThickness, SelectionRect.ActualWidth - strokeThickness * 2,
                                          SelectionRect.ActualHeight - strokeThickness * 2);
                Debug.WriteLine($"Srect H*W {_sectorRect.Height * _sectorRect.Width}");
                ImageDebugWindow sectorPreviewWindow = new ImageDebugWindow();
                sectorPreviewWindow.DebugImage.Source = ImageHelper.GetBitmapSourceFromSector(_sectorRect); 
                sectorPreviewWindow.Height = sectorPreviewWindow.DebugImage.Height + sectorPreviewWindow.TitleText.Height;
                sectorPreviewWindow.Width= sectorPreviewWindow.DebugImage.Width;
                sectorPreviewWindow.Title = "Seurattavan sektorin esikatselu";
                sectorPreviewWindow.Show();
                _areaToolWindowViewModel.SectorRect = _sectorRect;
                _areaToolWindowViewModel.SectorSelected = true; 
                Close();
            }
            else
            {
                _areaToolWindowViewModel.SectorRect = Rect.Empty;
                _areaToolWindowViewModel.SectorSelected = false;
                MessageBox.Show("Valittu alue liian pieni, valitse uudelleen.");
            }
        }
    }
}



