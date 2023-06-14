using System.Windows;

namespace ArtHoarderClient.View.Windows;

public partial class SortingWindow : Window
{
    public SortingWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}