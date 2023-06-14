using ArtHoarderClient.View.Windows;
using ArtHoarderClient.ViewModels;

namespace ArtHoarderClient;

public static class InputBoxDialog
{
    public static string? Show(string name, string tooltip)
    {
        var vm = new InputStringDialogViewModel
        {
            Title = name,
            ToolTip = tooltip
        };
        var dialog = new InputStringDialog { DataContext = vm };
        if (dialog.ShowDialog() ?? false)
            return vm.Input;
        return null;
    }
}