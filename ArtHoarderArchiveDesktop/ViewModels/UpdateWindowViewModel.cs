using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ArtHoarderClient.Infrastructure.Commands;
using ArtHoarderClient.View.Windows;
using ArtHoarderClient.ViewModels.Base;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.ViewModels;

internal class UpdateWindowViewModel : ViewModel
{
    public Archive Archive { get; set; } = null!;

    public UpdateWindowViewModel()
    {
        // UpdateAllCommand = new ActionCommand(OnUpdateAllExecuted, CanUpdateAllExecute);
        ClearSearchCommand = new ActionCommand(OnClearSearchExecuted, CanClearSearchExecute);
        UpdateSelectedCommand = new ActionCommand(OnUpdateSelectedExecuted, CanUpdateSelectedExecute);
    }

    public void Init() //TODO init check. add archManager to arg
    {
        _allProfiles = Archive.GetProfiles();
        DisplayedGalleries = _allProfiles;
    }

    private List<ProfileInfo> _allProfiles = null!;

    #region Props

    #region Title

    private string _title = "Update";

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    #endregion

    #region OutputLog

    //
    // private string _outputLog = "";
    //
    // public string OutputLog
    // {
    //     get => _outputLog;
    //     set => Set(ref _outputLog, value);
    // }
    //

    #endregion

    #region RB

    #region ByLocalName

    private bool? _byLocalName = true;

    public bool? ByLocalName
    {
        get => _byLocalName;
        set
        {
            Set(ref _byLocalName, value);
            if (value == true)
                UpdateSearch(SearchProperty.ByNickName);
        }
    }

    #endregion

    #region ByNickName

    private bool? _byNickName = false;

    public bool? ByNickName
    {
        get => _byNickName;
        set
        {
            Set(ref _byNickName, value);
            if (value == true)
                UpdateSearch(SearchProperty.ByNickName);
        }
    }

    #endregion

    #region ВyResource

    private bool? _byResource = false;

    public bool? ВyResource
    {
        get => _byResource;
        set
        {
            Set(ref _byResource, value);
            if (value == true)
                UpdateSearch(SearchProperty.ВyResource);
        }
    }

    #endregion

    #endregion

    #region SearchQuery

    private string _searchQuery = "";

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            Set(ref _searchQuery, value);

            if (ByNickName == true)
            {
                UpdateSearch(SearchProperty.ByNickName);
            }
            else if (ByLocalName == true)
            {
                UpdateSearch(SearchProperty.ByLocalName);
            }
            else if (ВyResource == true)
            {
                UpdateSearch(SearchProperty.ВyResource);
            }
        }
    }

    #endregion

    #region Title

    private bool? _checkAll = false;

    public bool? CheckAll
    {
        get => _checkAll;
        set { Set(ref _checkAll, value); }
    }

    #endregion

    #region DisplayedGalleries

    private List<ProfileInfo> _displayedGalleries = null!;

    public List<ProfileInfo> DisplayedGalleries
    {
        get => _displayedGalleries;
        private set => Set(ref _displayedGalleries, value);
    }

    #endregion

    #region LiteUpdate

    private bool _liteUpdate;

    public bool LiteUpdate
    {
        get => _liteUpdate;
        set => Set(ref _liteUpdate, value);
    }

    #endregion

    #endregion

    #region Commands

    #region UpdateSelected

    public ICommand UpdateSelectedCommand { get; }

    private bool CanUpdateSelectedExecute(object p) => true;

    private async void OnUpdateSelectedExecuted(object p)
    {
        var progressWindowViewModel = new ProgressWindowViewModel();
        var reporter = progressWindowViewModel.GetReporter();
        var progressWindow = new ProgressWindow { DataContext = progressWindowViewModel };

        progressWindow.Show();
        await Archive.UpdateGalleriesAsync(_allProfiles.Where(profile => profile.IsChecked ?? false).ToArray(),
            reporter).ConfigureAwait(false);
        progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
    }

    #endregion

    #region UpdateAll

    // public ICommand UpdateAllCommand { get; }
    //
    // private bool CanUpdateAllExecute(object p) => true;
    //
    // private void OnUpdateAllExecuted(object p)
    // {
    //     ArchiveManager.UpdateAllGalleriesAsync().ConfigureAwait(false);
    // }

    #endregion

    #region ClearSearch

    public ICommand ClearSearchCommand { get; }

    private bool CanClearSearchExecute(object p) => true;

    private void OnClearSearchExecuted(object p)
    {
        SearchQuery = "";
    }

    #endregion

    #endregion

    enum SearchProperty
    {
        ByNickName,
        ByLocalName,
        ВyResource
    }

    private void UpdateSearch(SearchProperty searchProperty)
    {
        if (SearchQuery == "")
        {
            DisplayedGalleries = _allProfiles;
            UpdateCheckAll();

            return;
        }

        DisplayedGalleries = searchProperty switch
        {
            SearchProperty.ByNickName => _allProfiles
                .Where(p => p.UserName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                .OrderBy(p => p.UserName?.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            SearchProperty.ByLocalName => _allProfiles
                .Where(p => p.OwnerName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.OwnerName.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            SearchProperty.ВyResource => _allProfiles
                .Where(p => p.Resource.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Resource.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            _ => _allProfiles
        };

        UpdateCheckAll();

        void UpdateCheckAll()
        {
            var length = DisplayedGalleries.Count;
            if (length == 0) return;
            if (length == 1) Set(ref _checkAll, DisplayedGalleries[0].IsChecked);

            var b = DisplayedGalleries[0].IsChecked;

            for (var i = 1; i < length; i++)
            {
                if (b != DisplayedGalleries[i].IsChecked)
                {
                    CheckAll = null;
                    return;
                }
            }

            CheckAll = b;
        }
    }
}