using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArtHoarderClient.View.Windows;

public partial class AddingWindow : Window
{
    public AddingWindow()
    {
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Tabs.SelectedItem is not TabItem tab) return;
        if (tab.Header.ToString() == "Add Subscriptions")
            Dispatcher.BeginInvoke(() => UsersUrlTextBox.Focus(), DispatcherPriority.ApplicationIdle);
        
    }
}