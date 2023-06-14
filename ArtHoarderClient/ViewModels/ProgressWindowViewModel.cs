using System.Windows;
using ArtHoarderClient.Infrastructure;
using ArtHoarderClient.ViewModels.Base;
using ArtHoarderCore.Infrastructure;

namespace ArtHoarderClient.ViewModels;

internal class ProgressWindowViewModel : ViewModel
{
    #region Props

    #region Stage

    private string _stage = "...";

    public string Stage
    {
        get => _stage;
        set => Set(ref _stage, value);
    }

    #endregion

    #region ProgressPercentage

    private double _progressPercentage = 100;

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => Set(ref _progressPercentage, value);
    }

    #endregion

    #region ProgressMaxPercentage

    private double _progressMaxPercentage = 100;

    public double ProgressMaxPercentage
    {
        get => _progressMaxPercentage;
        set => Set(ref _progressMaxPercentage, value);
    }

    #endregion

    #region Stage

    private string _logText = "";

    public string LogText
    {
        get => _logText;
        set => Set(ref _logText, value);
    }

    #endregion

    public ThreadSafeObservableCollection<SubProgressInfo> AdditionalProgressBars { get; } = new();

    #region IsAutoScrollToEnd

    private bool _isAutoScrollToEnd = true;

    public bool IsAutoScrollToEnd
    {
        get => _isAutoScrollToEnd;
        set => Set(ref _isAutoScrollToEnd, value);
    }

    #endregion

    #endregion


    public ProgressReporter GetReporter()
    {
        return new ProgressReporter(stage => Stage = stage,
            value => ProgressPercentage = value,
            value => ProgressMaxPercentage = value,
            message => LogText += message + '\n',
            subBar => AdditionalProgressBars.Add(subBar),
            subBar => AdditionalProgressBars.Remove(subBar),
            ShowError);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}