using ArtHoarderClient.ViewModels.Base;

namespace ArtHoarderClient.ViewModels;

internal class ImageBrowserViewModel : ViewModel
{
    #region Props

    #region MyRegion

    #region Title

    private string _title = "Image";

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    #endregion

    #endregion

    #endregion
}