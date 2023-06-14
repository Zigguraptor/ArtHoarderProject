using ArtHoarderClient.ViewModels.Base;

namespace ArtHoarderClient.ViewModels;

internal class InputStringDialogViewModel : ViewModel
{
    #region Props

    #region Title

    private string _title = "input";

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    #endregion

    #region ToolTip

    private string _toolTip = "Text:";

    public string ToolTip
    {
        get => _toolTip;
        set => Set(ref _toolTip, value);
    }

    #endregion

    #region Input

    private string _input = "";

    public string Input
    {
        get => _input;
        set => Set(ref _input, value);
    }

    #endregion

    #endregion
}