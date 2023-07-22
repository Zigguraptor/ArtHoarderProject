using System.Windows;

namespace ArtHoarderClient.View.Windows;

public partial class InputStringDialog : Window
{
    public InputStringDialog()
    {
        InitializeComponent();
    }

    private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}