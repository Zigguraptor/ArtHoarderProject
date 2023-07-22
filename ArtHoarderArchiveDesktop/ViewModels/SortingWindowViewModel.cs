using System.Collections.Generic;
using ArtHoarderClient.ViewModels.Base;

namespace ArtHoarderClient.ViewModels;

internal class SortingWindowViewModel : ViewModel
{
    public SortingWindowViewModel()
    {
    }

    public SortingWindowViewModel(IEnumerable<string> availableAlgorithms)
    {
        _availableAlgorithms = availableAlgorithms;
        // ApplyCommand = new ActionCommand(OnApplyExecuted, CanApplyExecute);
    }

    #region Props

    #region Title

    private string _title = "Advanced sorting";


    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    #endregion

    #region AvailableAlgorithms

    private IEnumerable<string> _availableAlgorithms;

    public IEnumerable<string> AvailableAlgorithms
    {
        get => _availableAlgorithms;
        set => Set(ref _availableAlgorithms, value);
    }

    #endregion

    #region SelectedAlgorithm

    private string _selectedAlgorithm = null!;

    public string SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set => Set(ref _selectedAlgorithm, value);
    }

    #endregion

    #endregion

    #region Commands

    #region Apply

    // public ICommand ApplyCommand { get; }
    //
    // private bool CanApplyExecute(object p) => true;
    //
    // private async void OnApplyExecuted(object p)
    // {
    // }

    #endregion

    #endregion
}