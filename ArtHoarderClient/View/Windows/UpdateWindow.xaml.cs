using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderClient.View.Windows;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.ItemsSource is ProfileInfo[] displayedGalleries)
        {
            foreach (var gallery in displayedGalleries)
                gallery.IsChecked = true;

            UsersGrid.Items.Refresh();
        }
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.ItemsSource is ProfileInfo[] displayedGalleries)
        {
            foreach (var gallery in displayedGalleries)
                gallery.IsChecked = false;

            UsersGrid.Items.Refresh();
        }
    }

    private void UpdateCheckAll(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.ItemsSource is not ProfileInfo[] displayedGalleries) return;
        if (UsersGrid.Columns.Last().Header is not CheckBox checkBox) return;

        var length = displayedGalleries?.Length;

        if (length == 0)
            return;
        if (length == 1)
            checkBox.IsChecked = displayedGalleries?[0].IsChecked;

        var b = displayedGalleries?[0].IsChecked;

        for (var i = 1; i < length; i++)
        {
            if (b != displayedGalleries?[i].IsChecked)
            {
                checkBox.IsChecked = null;
                return;
            }
        }

        checkBox.IsChecked = b;
    }
}