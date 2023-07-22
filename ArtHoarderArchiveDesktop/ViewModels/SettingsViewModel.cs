using ArtHoarderClient.ViewModels.Base;

namespace ArtHoarderClient.ViewModels;

internal class SettingsViewModel : ViewModel
{
    #region Props

    #region MyRegion

    #region Title

    private string _title = "Settings";

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    #endregion

    #endregion

    #endregion
}