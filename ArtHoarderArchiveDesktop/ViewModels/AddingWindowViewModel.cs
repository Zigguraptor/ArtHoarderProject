using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArtHoarderClient.Infrastructure.Commands;
using ArtHoarderClient.Models;
using ArtHoarderClient.View.Windows;
using ArtHoarderClient.ViewModels.Base;
using ArtHoarderCore.Managers;
using Microsoft.Win32;

namespace ArtHoarderClient.ViewModels;

internal class AddingWindowViewModel : ViewModel
{
    public AddingWindowViewModel()
    {
        AddAllToArchiveCommand = new ActionCommand(OnAddAllToArchiveExecuted, CanAddAllToArchiveExecute);
        PastLinksCommand = new ActionCommand(OnPastLinksExecuted, CanPastLinksExecute);
        LoadFilesCommand = new ActionCommand(OnLoadFilesExecuted, CanLoadFilesExecute);
        AddSubscriptionsCommand = new ActionCommand(OnAddSubscriptionsExecuted, CanAddSubscriptionsExecute);
    }

    public Archive? ArchiveManager { get; set; }

    #region Props

    #region SelectedTab

    private int _selectedTab = 0;

    public int SelectedTab
    {
        get => _selectedTab;
        set => Set(ref _selectedTab, value);
    }

    #endregion

    #region UserName

    private string _userName = "";

    public string UserName
    {
        get => _userName;
        set => Set(ref _userName, value);
    }

    #endregion

    #region GalleryUri

    private string _galleryUri = "";

    public string GalleryUri
    {
        get => _galleryUri;
        set => Set(ref _galleryUri, value);
    }

    #endregion

    #region DownloadAfterAdding

    private bool _downloadAfterAdding = true;

    public bool DownloadAfterAdding
    {
        get => _downloadAfterAdding;
        set => Set(ref _downloadAfterAdding, value);
    }

    #endregion

    #region Users

    private List<string> _users;

    public List<string> Users
    {
        get => _users;
        set => Set(ref _users, value);
    }

    #endregion

    #region Galleryes

    private ObservableCollection<Gallery>? _galleries;

    public ObservableCollection<Gallery>? Galleries
    {
        get => _galleries;
        set => Set(ref _galleries, value);
    }

    #endregion

    #region AddingResults

    private string _subscriptionsResults = "";

    public string SubscriptionsResults
    {
        get => _subscriptionsResults;
        set => Set(ref _subscriptionsResults, value);
    }

    #endregion

    #region ProfileForExtractSubscriptions

    private string _profileForExtractSubscriptions = "";

    public string ProfileForExtractSubscriptions
    {
        get => _profileForExtractSubscriptions;
        set => Set(ref _profileForExtractSubscriptions, value);
    }

    #endregion

    #endregion


    #region Commands

    #region AddAllToArchive

    public ICommand AddAllToArchiveCommand { get; }

    private bool CanAddAllToArchiveExecute(object p) => Galleries != null;

    private async void OnAddAllToArchiveExecuted(object p)
    {
        if (Galleries == null)
            return;

        var galleriesLength = Galleries.Count;
        var progressWindowViewModel = new ProgressWindowViewModel();
        var reporter = progressWindowViewModel.GetReporter();
        var progressWindow = new ProgressWindow { DataContext = progressWindowViewModel };

        reporter.SetProgressStage("Adding galleries...");
        reporter.SetProgressBar(0, galleriesLength);

        progressWindow.Show();

        var success = 0;
        var fail = 0;
        await Parallel.ForEachAsync(Galleries, (gallery, _) =>
        {
            if (ArchiveManager!.TryAddNewGallery(gallery.GalleryProfileUri, gallery.OwnerName ?? string.Empty))
            {
                success++;
                reporter.ReportAndProgress($"{gallery.GalleryProfileUri} Added success.");
            }
            else
            {
                fail++;
                reporter.ReportAndProgress($"{gallery.GalleryProfileUri} Add fail.");
            }

            return new ValueTask(Task.CompletedTask);
        });

        MessageBox.Show($"{success} Profiles added. {fail} Failed.", "Success", MessageBoxButton.OK,
            MessageBoxImage.Information); //TODO

        if (DownloadAfterAdding)
        {
            reporter.SetProgressStage("Galleries Updating");
            reporter.SetProgressBar(0, galleriesLength);

            success = 0;
            fail = 0;
            foreach (var gallery in Galleries)
            {
                reporter.Progress();
                if (await ArchiveManager!
                        .UpdateGalleryAsync(gallery.GalleryProfileUri, gallery.OwnerName ?? string.Empty, reporter)
                        .ConfigureAwait(false))
                {
                    success++;
                }
                else
                {
                    fail++;
                }
            }

            MessageBox.Show($"{success} Updated. {fail} Failed.", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        Galleries = null;
        SubscriptionsResults = "";
        progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
    }

    #endregion

    #region PastLinks

    public ICommand PastLinksCommand { get; }

    private bool CanPastLinksExecute(object p) => true;

    private void OnPastLinksExecuted(object p)
    {
        LoadLinks(Clipboard.GetText().Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    #endregion

    #region LoadFiles

    public ICommand LoadFilesCommand { get; }

    private bool CanLoadFilesExecute(object p) => true;

    private void OnLoadFilesExecuted(object p)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Multiselect = true;
        if (openFileDialog.ShowDialog() != true) return;

        var lines = new List<string>();
        foreach (var fileName in openFileDialog.FileNames)
            lines.AddRange(File.ReadAllText(fileName).Split('\n', StringSplitOptions.RemoveEmptyEntries));

        LoadLinks(lines.ToArray());
    }

    #endregion

    #region AddSubscriptions

    public ICommand AddSubscriptionsCommand { get; }

    private bool CanAddSubscriptionsExecute(object p) => true;

    private async void OnAddSubscriptionsExecuted(object p)
    {
        Uri? profileUri = null;
        try
        {
            profileUri = new Uri(ProfileForExtractSubscriptions);
        }
        catch //TODO error
        {
            MessageBox.Show("Uri not uri", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (profileUri == null)
            return;

        var progressWindowViewModel = new ProgressWindowViewModel();
        var reporter = progressWindowViewModel.GetReporter();
        var progressWindow = new ProgressWindow { DataContext = progressWindowViewModel };

        var urisTask = ArchiveManager?.TryGetSubscriptionsAsync(profileUri, reporter);
        if (urisTask != null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            progressWindow.Closing += (_, _) => cancellationTokenSource.Cancel();
            progressWindow.Show();

            var uris = await urisTask.ConfigureAwait(false);
            if (reporter.IsError) return;

            var galleries = uris.Select(uri => new Gallery(uri, ArchiveManager!.TryGetUserName(uri))).ToArray();

            SubscriptionsResults = $"Found {galleries.Length} links";
            Galleries = new ObservableCollection<Gallery>(galleries);
            SelectedTab = 0;

            progressWindow.Dispatcher.Invoke(() => progressWindow.Close());

            return;
        }

        MessageBox.Show("Probably parser not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    #endregion

    #endregion

    private void LoadLinks(string[] lines)
    {
        var uris = new List<Uri>(lines.Length);
        foreach (var s in lines)
        {
            try
            {
                uris.Add(new Uri(s));
            }
            catch
            {
                // ignored
            }
        }

        var result = uris.Where(uri => ArchiveManager!.CheckLink(uri))
            .Select(uri => new Gallery(uri, ArchiveManager!.TryGetUserName(uri))).ToArray();

        var linesLength = lines.Length;
        var urisCount = uris.Count;

        SubscriptionsResults = CreateResult(linesLength, linesLength - urisCount, urisCount - result.Length);

        if (result.Length <= 0) return; //Galleries = new ObservableCollection<Gallery>(result);

        Galleries ??= new ObservableCollection<Gallery>();
        foreach (var gallery in result)
            Galleries.Add(gallery);
    }

    private string CreateResult(int totalLines, int notLinks, int noSupported)
    {
        var r = $"Lines read: {totalLines}";
        if (notLinks > 0)
        {
            r += $"; Not identified: {notLinks}";
        }

        if (noSupported > 0)
        {
            r += $"; resource not supported: {noSupported}";
        }

        return r;
    }
}