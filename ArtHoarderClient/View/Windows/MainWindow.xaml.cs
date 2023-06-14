using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ArtHoarderClient.View.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double PanelMinWidth = 150;
        private const double PanelDefaultWidth = 300;

        private double _rightPanelWidth;
        private double _leftPanelWidth;

        public MainWindow()
        {
            InitializeComponent();
            _rightPanelWidth = PanelDefaultWidth;
            _leftPanelWidth = PanelDefaultWidth;

            // RightPanel.Width = _rightPanelWidth;
            // LeftPanel.Width = _leftPanelWidth;
        }

        private void SwitchRightPanel(object sender, RoutedEventArgs e)
        {
            if (RightPanel.Visibility == Visibility.Visible)
            {
                _rightPanelWidth = ThirdColumn.Width.Value;
                RightSplitter.Visibility = Visibility.Collapsed;
                RightPanel.Visibility = Visibility.Collapsed;
                ThirdColumn.Width = GridLength.Auto;
            }
            else
            {
                RightSplitter.Visibility = Visibility.Visible;
                RightPanel.Visibility = Visibility.Visible;
                ThirdColumn.Width = new GridLength(_rightPanelWidth);
            }
        }

        private void SwitchLeftPanel(object sender, RoutedEventArgs e)
        {
            if (LeftPanel.Visibility == Visibility.Visible)
            {
                _leftPanelWidth = FirstColumn.Width.Value;
                LeftSplitter.Visibility = Visibility.Collapsed;
                LeftPanel.Visibility = Visibility.Collapsed;
                FirstColumn.Width = GridLength.Auto;
            }
            else
            {
                LeftSplitter.Visibility = Visibility.Visible;
                LeftPanel.Visibility = Visibility.Visible;
                LeftPanel.Width = _leftPanelWidth;
                FirstColumn.Width = new GridLength(_leftPanelWidth);
            }
        }
    }
}