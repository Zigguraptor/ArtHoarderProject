using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ArtHoarderClient.Infrastructure;
using ArtHoarderClient.Infrastructure.Commands;
using ArtHoarderClient.Infrastructure.FoldersAndItems;
using ArtHoarderClient.Models;
using ArtHoarderClient.View.Windows;
using ArtHoarderClient.ViewModels.Base;
using Microsoft.Win32;

// ReSharper disable ObjectCreationAsStatement

namespace ArtHoarderClient.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        public MainWindowViewModel()
        {
            #region Commands

            CloseApplicationCommand =
                new ActionCommand(OnCloseApplicationCommandExecuted, CanCloseApplicationCommandExecute);
            CreateArchiveCommand = new ActionCommand(OnCreateArchiveExecuted, CanCreateArchiveExecute);
            OpenArchiveCommand = new ActionCommand(OnCOpenArchiveCommandExecuted, CanOpenArchiveCommandExecute);
            OpenAddingWindowCommand = new ActionCommand(OnAddingWindowOpenExecuted, CanAddingWindowOpenExecute);
            OpenUpdateWindowCommand = new ActionCommand(OnOpenUpdateWindowExecuted, CanOpenUpdateWindowExecute);
            OpenSortingWindowCommand = new ActionCommand(OnOpenSortingWindowExecuted, CanOpenSortingWindowExecute);

            OpenItemCommand = new ActionCommand(OnOpenItemExecuted, CanOpenItemExecute);
            BackCommand = new ActionCommand(OnBackCommandExecuted, CanBackCommandExecute);
            UpdateExplorerCommand = new ActionCommand(OnUpdateExplorerCommandExecuted, CanUpdateExplorerCommandExecute);
            ChangeDisplayModeCommand = new ActionCommand<DisplayMode>(OnDisplayModeExecuted, CanDisplayModeExecute);
            CreateFolderCommand = new ActionCommand(OnCreateFolderExecuted, CanCreateFolderExecute);

            CopySubmissionPropsCommand =
                new ActionCommand(OnCopySubmissionPropsExecuted, CanCopySubmissionPropsExecute);

            #endregion

            var problems = ProjectAnalyzer.AnalyzeProject();
            if (problems != null)
                MessageBox.Show(problems);
        }

        public string? UsersFoldersTemplatePath =>
            ArchiveManager?.MetaDataFolder != null
                ? Path.Combine(ArchiveManager.MetaDataFolder, "User folders template.json")
                : null;

        private ViewItemsTree? Tree { get; set; }

        private Archive? _archiveManager;

        private Archive? ArchiveManager
        {
            get => _archiveManager;
            set
            {
                if (_archiveManager == value) return;
                _archiveManager = value;
                if (ArchiveManager == null)
                {
                    Title = "Art hoarder archive";
                    return;
                }

                Title = ArchiveManager.ArchiveName + " — Art hoarder archive";
                var root = new DynamicFolderNode(ArchiveManager.ArchiveName) { Template = DefaultFoldersTemplate };
                Tree = new ViewItemsTree(root);
                UpdateViewItems();
            }
        }

        #region Props

        #region Title

        private string _title = "Art hoarder archive";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        #endregion

        #region IsHitTestVisible

        private bool _isHitTestVisible = true;

        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set => Set(ref _isHitTestVisible, value);
        }

        #endregion

        #region DisplayMode

        private DisplayMode _displayMode = DisplayMode.Medium;

        public DisplayMode DisplayMode
        {
            get => _displayMode;
            set => Set(ref _displayMode, value);
        }

        #endregion

        #region Breadcrumb

        private List<BaseNode>? _breadcrumbs;

        public List<BaseNode>? Breadcrumbs
        {
            get => _breadcrumbs;
            private set => Set(ref _breadcrumbs, value);
        }

        #endregion

        #region DisplayedElements

        private List<IDisplayViewItem>? _displayedElements;

        public List<IDisplayViewItem>? DisplayedElements
        {
            get => _displayedElements;
            private set => Set(ref _displayedElements, value);
        }

        #endregion

        #region SelectedItem

        private IDisplayViewItem? _selectedItem;

        public IDisplayViewItem? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        #endregion

        #region SelectedSubmissionProps

        private IList? _selectedSubmissionProps;

        public IList? SelectedSubmissionProps
        {
            get => _selectedSubmissionProps;
            set => Set(ref _selectedSubmissionProps, value);
        }

        #endregion

        #region LeftPanelData

        private IPanelData? _leftPanelData;

        public IPanelData? LeftPanelData
        {
            get => _leftPanelData;
            set => Set(ref _leftPanelData, value);
        }

        #endregion

        #region RightPanelWidth

        private double _rightPanelWidth;

        public double RightPanelWidth
        {
            get => _rightPanelWidth;
            set => Set(ref _rightPanelWidth, value);
        }

        #endregion

        #region RightPanelData

        private IPanelData? _rightPanelData = new PanelSubmissionInfo();

        public IPanelData? RightPanelData
        {
            get => _rightPanelData;
            set => Set(ref _rightPanelData, value);
        }

        #endregion

        #endregion

        #region Commands

        #region CloseApp

        public ICommand CloseApplicationCommand { get; }

        private bool CanCloseApplicationCommandExecute(object p) => true;

        private void OnCloseApplicationCommandExecuted(object p)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region DisplayMode

        public ICommand ChangeDisplayModeCommand { get; }

        private bool CanDisplayModeExecute(DisplayMode mode) => ArchiveManager != null;

        private void OnDisplayModeExecuted(DisplayMode mode)
        {
            if (DisplayMode != mode)
                DisplayMode = mode;
        }

        #endregion

        #region CreateArchive

        public ICommand CreateArchiveCommand { get; }

        private bool CanCreateArchiveExecute(object p) => true;

        private void OnCreateArchiveExecuted(object p)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arts archives files (*.aharch)|*.aharch",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, "Windows forms sucks");
                OpenArchive(saveFileDialog.FileName);
            }
        }

        #endregion

        #region OpenArchive

        public ICommand OpenArchiveCommand { get; }

        private bool CanOpenArchiveCommandExecute(object p) => true;

        private void OnCOpenArchiveCommandExecuted(object p)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Arts archives files (*.aharch)|*.aharch",
                FilterIndex = 1,
            };
            if (dlg.ShowDialog() == false) return;

            IsHitTestVisible = false;
            OpenArchive(dlg.FileName);
            IsHitTestVisible = true;
        }

        #endregion

        #region OpenUpdateWindow

        public ICommand OpenUpdateWindowCommand { get; }

        private bool CanOpenUpdateWindowExecute(object p) => ArchiveManager != null;

        private void OnOpenUpdateWindowExecuted(object p)
        {
            var updateWindow = new UpdateWindow();

            var updateWindowViewModel = updateWindow.DataContext as UpdateWindowViewModel;
            updateWindowViewModel!.Archive = ArchiveManager!;
            updateWindowViewModel.Init();

            updateWindow.ShowDialog();
        }

        #endregion

        #region AddingWindowOpen

        public ICommand OpenAddingWindowCommand { get; }

        private bool CanAddingWindowOpenExecute(object p) => ArchiveManager != null;

        private void OnAddingWindowOpenExecuted(object p)
        {
            var addingWindow = new AddingWindow();

            var addingWindowViewModel = addingWindow.DataContext as AddingWindowViewModel;
            addingWindowViewModel!.ArchiveManager = ArchiveManager!;
            addingWindowViewModel.Users = ArchiveManager!.GetUsers().Select((user => user.Name)).ToList();

            addingWindow.ShowDialog();
        }

        #endregion

        #region UpdateExplorer

        public ICommand UpdateExplorerCommand { get; }

        private bool CanUpdateExplorerCommandExecute(object p) => ArchiveManager != null;

        private void OnUpdateExplorerCommandExecuted(object p)
        {
            UpdateViewItems();
        }

        #endregion

        #region OpenItem

        public ICommand OpenItemCommand { get; }

        private bool CanOpenItemExecute(object p) => Tree != null;

        private void OnOpenItemExecuted(object p)
        {
            if (Tree == null || p is not FolderNode node) return;
            if (!Tree.TryEnterTo(node))
                Tree.TryGoBackTo(node);


            UpdateViewItems();
        }

        #endregion

        #region Back

        public ICommand BackCommand { get; }

        private bool CanBackCommandExecute(object p) => Tree?.Current.Parent != null;

        private void OnBackCommandExecuted(object p)
        {
            if (Tree == null || !Tree.TryGoBack()) return;
            UpdateViewItems();
        }

        #endregion

        #region OpenSortingWindow

        public ICommand OpenSortingWindowCommand { get; }

        private bool CanOpenSortingWindowExecute(object p) =>
            Tree != null && Tree.Current.Children.Count < DisplayedElements?.Count;

        private void OnOpenSortingWindowExecuted(object p)
        {
            if (ArchiveManager == null) return;

            var viewModel = new SortingWindowViewModel(ArchiveManager.PHashAlgorithmsInDb);
            var sortingDialog = new SortingWindow
            {
                DataContext = viewModel
            };
            var result = sortingDialog.ShowDialog();
            if (result == null || !result.Value) return;
            //TODO
        }

        #endregion

        #region CreateFolder

        public ICommand CreateFolderCommand { get; }

        private bool CanCreateFolderExecute(object p) => Tree is { Current: CustomFolderNode };

        private void OnCreateFolderExecuted(object p)
        {
            if (ArchiveManager == null || Tree is not { Current: CustomFolderNode }) return;

            var input = InputBoxDialog.Show("Name", "Enter folder name:");
            if (input == null) return;

            Tree.Current.Children.Add(new CustomFolderNode(input, Tree.Current));
            UpdateViewItems();

            if (UsersFoldersTemplatePath == null)
            {
                MessageBox.Show("Saving error. No path.", "Error!", MessageBoxButton.OK,
                    MessageBoxImage.Error); //TODO add log
                return;
            }

            try
            {
                using var stream = File.Open(UsersFoldersTemplatePath, FileMode.OpenOrCreate);
                stream.Position = 0;
                JsonSerializer.Serialize(stream, Tree.Current, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                MessageBox.Show("Failed to save custom folders.", "Error!", MessageBoxButton.OK,
                    MessageBoxImage.Error); //TODO add log
            }
        }

        #endregion

        #region CopySubmissionProps

        public ICommand CopySubmissionPropsCommand { get; }

        private bool CanCopySubmissionPropsExecute(object p)
        {
            return SelectedSubmissionProps?.Count > 0;
        }

        private void OnCopySubmissionPropsExecuted(object p)
        {
            if (SelectedSubmissionProps?.Count == 1)
            {
                var prop = SelectedSubmissionProps[0] as Property;
                Clipboard.SetText($"\"{prop?.Name}\": \"{prop?.Value}\"");
                return;
            }

            var result = (from object? prop in SelectedSubmissionProps select prop as Property).Aggregate("{\n",
                (current, property) => current + $"  \"{property?.Name}\": \"{property?.Value}\",\n");
            result += "}";
            Clipboard.SetText(result);
        }

        #endregion

        #endregion

        private void OpenArchive(string path)
        {
            var fileText = File.ReadAllText(path);
            if (fileText != "Windows forms sucks")
            {
                IsHitTestVisible = true;
                return;
            }

            var progressWindowViewModel = new ProgressWindowViewModel();
            var reporter = progressWindowViewModel.GetReporter();
            var progressWindow = new ProgressWindow { DataContext = progressWindowViewModel };

            progressWindow.Show();

            reporter.Report("Loading archive");
            ArchiveManager = new Archive(path, reporter);

            progressWindow.Dispatcher.Invoke(() => progressWindow.Close());
        }

        private void UpdateViewItems()
        {
            if (ArchiveManager == null || Tree == null) return;

            DisplayedElements = Tree.Current.GetViewItems(ArchiveManager);
            Breadcrumbs = Tree.Current.GetBreadcrumbs();
        }

        #region Templates

        private void DefaultFoldersTemplate(DynamicFolderNode parent)
        {
            if (ArchiveManager == null) return;

            if (File.Exists(UsersFoldersTemplatePath)) //Deserialization //TODO optimize
            {
                try
                {
                    var text = File.ReadAllText(UsersFoldersTemplatePath);
                    var rootUserFolder =
                        JsonSerializer.Deserialize<CustomFolderNode>(text);
                    if (rootUserFolder != null)
                    {
                        rootUserFolder.Parent = parent;
                        parent.Children.Add(rootUserFolder);
                    }
                }
                catch //TODO log and create new file "UsersFoldersTemplatePath"
                {
                    MessageBox.Show("Failed to load custom folders", "Warning!", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    parent.Children.Add(new CustomFolderNode("User's folders", parent));
                }
            }
            else
            {
                parent.Children.Add(new CustomFolderNode("User's folders", parent));
            }

            parent.Children.Add(new DynamicFolderNode("Sorted by user name", parent)
                { Template = TemplateByLocalName });
            parent.Children.Add(new DynamicFolderNode("Unregistered files", parent)
                { Template = TemplateFiles(ArchiveManager.UnregisteredFiles) });
            parent.Children.Add(new DynamicFolderNode("Missing files", parent)
                { Template = TemplateFiles(ArchiveManager.MissingFiles) });
            parent.Children.Add(new DynamicFolderNode("Changed files", parent)
                { Template = TemplateFiles(ArchiveManager.ChangedFiles) });
        }

        private void TemplateByLocalName(DynamicFolderNode parent)
        {
            if (ArchiveManager == null) return;
            var users = ArchiveManager.GetUsers();
            foreach (var user in users)
            {
                parent.Children.Add(new DynamicFolderNode(user.Name, parent)
                {
                    Template = TemplateByResource(user)
                });
            }
        }

        private Action<DynamicFolderNode> TemplateByResource(User user)
        {
            if (ArchiveManager == null) return _ => { };
            return delegate(DynamicFolderNode parent)
            {
                var profiles = ArchiveManager.GetProfiles(info => info.OwnerName == user.Name);
                var resources = profiles.Select(info => info.Resource).Distinct();
                foreach (var resource in resources)
                {
                    parent.Children.Add(new DynamicFolderNode(resource, parent)
                    {
                        Template = TemplateByGalleryProfiles(profiles.Where(profileInfo =>
                            profileInfo.Resource == resource))
                    });
                }
            };
        }

        private Action<DynamicFolderNode> TemplateByGalleryProfiles(IEnumerable<ProfileInfo> profileInfos)
        {
            return delegate(DynamicFolderNode parent)
            {
                if (ArchiveManager == null) return;

                foreach (var profileInfo in profileInfos)
                {
                    var submissions =
                        ArchiveManager.GetSubmissions(submission => submission.SourceGalleryUri == profileInfo.Uri);

                    parent.Children.Add(new DynamicFolderNode(profileInfo.UserName ?? "Unknown", parent)
                        { Template = TemplateSubmissions(submissions) });
                }
            };
        }

        private Action<DynamicFolderNode> TemplateSubmissions(List<FullSubmissionInfo> fullSubmissionInfos)
        {
            return delegate(DynamicFolderNode parent)
            {
                foreach (var submission in fullSubmissionInfos)
                    parent.Children.Add(new SubmissionNode(submission, parent));
            };
        }

        private Action<DynamicFolderNode> TemplateFiles(List<FileMetaInfo> fileMetaInfos)
        {
            return delegate(DynamicFolderNode parent)
            {
                foreach (var fileMetaInfo in fileMetaInfos)
                    parent.Children.Add(new FileNode(fileMetaInfo, parent));
            };
        }

        private Action<DynamicFolderNode> TemplateFiles(IEnumerable<string> filePaths)
        {
            return delegate(DynamicFolderNode parent)
            {
                foreach (var path in filePaths)
                    parent.Children.Add(new FileNode(path, parent));
            };
        }

        #endregion
    }
}
