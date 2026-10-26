using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Components;
using SecureBox.Config;
using SecureBox.Services.Cloud;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public abstract partial class ItemsViewModel(
    SecureBoxStore secureBoxStore,
    string userId,
    INavigationService addSecretNavigationService,
    ParameterNavigationService<FileItemViewModel, OpenSecretViewModel> openFileItemNavigationService,
    ICloudService cloud,
    CloudSyncService cloudSync
) : ViewModelBase
{
    internal readonly SecureBoxStore _secureBoxStore = secureBoxStore;

    private readonly string _userId = userId;
    private readonly ParameterNavigationService<
        FileItemViewModel,
        OpenSecretViewModel
    > _openFileItemNavigationService = openFileItemNavigationService;

    public bool FavoriteView { get; protected set; }

    [ObservableProperty]
    public partial FileItemViewModel? SelectedItem { get; set; }

    public ObservableCollection<FileItemViewModel> AllItems { get; protected set; } = [];
    public ObservableCollection<FileItemViewModel> Directories { get; protected set; } = [];
    public ObservableCollection<FileItemViewModel> Files { get; protected set; } = [];

    public bool HasDirectories => Directories.Count > 0;
    public bool HasFiles => Files.Count > 0;

    public ObservableCollection<BreadCrumbItem> BreadCrumbItems { get; } = [];

    // Folder opened from the mosaic view, or null while the root items are shown
    private string? _currentFolder;

    private string UserFolder => Path.Combine(VaultConfig.Instance.Vault, _userId);

    /// <summary>
    /// Name of the first breadcrumb item, which leads back to the root items
    /// </summary>
    protected virtual string RootName => _userId;

    [ObservableProperty]
    public partial string? Filter { get; set; }

    partial void OnFilterChanged(string? value)
    {
        foreach (var item in AllItems)
            item.Filter(value);
    }

    // The view model is created on every navigation, so the settings are read
    // once and a change made in the settings applies the next time it is shown
    public bool IsTreeView { get; } = VaultConfig.Instance.DataView == DataViewMode.TreeView;
    public bool IsMosaicView => !IsTreeView;
    public int MosaicColumns { get; } = Math.Clamp(VaultConfig.Instance.MosaicColumns, 1, 12);

    [RelayCommand]
    private void DoubleClick(FileItemViewModel parameter)
    {
        if (parameter.Type == "Folder")
        {
            OpenFolder(parameter.FilePath);
        }
        else if (parameter.Type == "File")
        {
            _openFileItemNavigationService.Navigate(parameter);
        }
    }

    [RelayCommand]
    private void AddSecret() => addSecretNavigationService.Navigate();

    #region Cloud

    [ObservableProperty]
    public partial string? CloudStatus { get; set; }

    private bool CanUseCloud => cloud.IsSignedIn;

    [RelayCommand(CanExecute = nameof(CanUseCloud))]
    private Task SyncSecrets() => RunCloud("Syncing…", cloudSync.SyncAsync);

    [RelayCommand(CanExecute = nameof(CanUseCloud))]
    private Task UploadSecrets() => RunCloud("Uploading…", cloudSync.UploadAsync);

    [RelayCommand(CanExecute = nameof(CanUseCloud))]
    private Task DownloadSecrets() => RunCloud("Downloading…", cloudSync.DownloadAsync);

    private async Task RunCloud(
        string progress,
        Func<CancellationToken, Task<CloudSyncResult>> operation
    )
    {
        CloudStatus = progress;
        try
        {
            CloudStatus = (await operation(CancellationToken.None)).ToString();
        }
        catch (CloudException ex)
        {
            CloudStatus = ex.Message;
        }
    }

    /// <summary>
    /// Starts following the cloud sign in, so that the cloud commands are only
    /// enabled while a cloud account is signed in
    /// </summary>
    protected void ObserveCloud() => cloud.SignedInChanged += Cloud_SignedInChanged;

    public override void Dispose()
    {
        cloud.SignedInChanged -= Cloud_SignedInChanged;
        base.Dispose();
    }

    private void Cloud_SignedInChanged()
    {
        SyncSecretsCommand.NotifyCanExecuteChanged();
        UploadSecretsCommand.NotifyCanExecuteChanged();
        DownloadSecretsCommand.NotifyCanExecuteChanged();
        CloudStatus = null;
    }

    #endregion

    [RelayCommand]
    public void RemoveSecret()
    {
        if (SelectedItem is null)
            return;

        _secureBoxStore.RemoveItem(SelectedItem);
    }

    /// <summary>
    /// Loads the items shown when no folder is open
    /// </summary>
    protected abstract void LoadRoot();

    /// <summary>
    /// Reloads the items after the store changed, staying in the open folder
    /// while it still exists
    /// </summary>
    protected void Refresh()
    {
        if (_currentFolder is not null && !Directory.Exists(_currentFolder))
            _currentFolder = null;

        OpenFolder(_currentFolder);
    }

    /// <summary>
    /// Shows the content of a folder, or the root items when the folder is null
    /// </summary>
    private void OpenFolder(string? folder)
    {
        _currentFolder = folder;

        if (folder is null)
        {
            LoadRoot();
        }
        else
        {
            List<FileItemViewModel> allItems = [];
            _secureBoxStore.GetFiles(folder, allItems, [], 0);
            AllItems = [.. allItems];
            Directories = [.. allItems.Where(i => i.Type == "Folder")];
            Files = [.. allItems.Where(i => i.Type == "File")];
        }

        foreach (var item in AllItems)
            item.Filter(Filter);

        SelectedItem = null;
        BuildBreadCrumb();

        OnPropertyChanged(nameof(AllItems));
        OnPropertyChanged(nameof(Directories));
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(HasDirectories));
        OnPropertyChanged(nameof(HasFiles));
    }

    private void BuildBreadCrumb()
    {
        // The items are not observable, so they are complete before being shown
        List<BreadCrumbItem> items =
        [
            new()
            {
                DisplayName = RootName,
                NavigateCommand = new RelayCommand(() => OpenFolder(null)),
            },
        ];

        if (_currentFolder is not null)
        {
            string relative = Path.GetRelativePath(UserFolder, _currentFolder);
            string[] segments = relative.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries
            );

            string path = UserFolder;
            foreach (string segment in segments)
            {
                path = Path.Combine(path, segment);
                string target = path;

                items.Add(
                    new()
                    {
                        DisplayName = segment,
                        NavigationPath = target,
                        NavigateCommand = new RelayCommand(() => OpenFolder(target)),
                    }
                );
            }
        }

        items[^1].IsLast = true;

        BreadCrumbItems.Clear();
        foreach (var item in items)
            BreadCrumbItems.Add(item);
    }

    //    foreach (var item in items)
    //    {
    //        if (item.Type == "Folder")
    //        {
    //            _ = Task.Run(() => UploadItemsList(item.SubItems));
    //        }
    //        else
    //        {
    //            using (StreamReader inputStream = new StreamReader(item.Path))
    //            {
    //                byte[] content;
    //                using (var memoryStream = new MemoryStream())
    //                {
    //                    inputStream.BaseStream.Position = 0;
    //                    await inputStream.BaseStream.CopyToAsync(memoryStream);
    //                    content = memoryStream.ToArray();
    //                }
    //                _ = FirebaseAuth.SaveSecretFirestore(
    //                    _firebaseStore.Data.Uid,
    //                    _firebaseStore.Data.IdToken,
    //                    _userId,
    //                    item.RelativePath,
    //                    content
    //                );
    //            }
    //        }
    //    }
    //}

    //    List<FirestoreDocument> list = await FirebaseAuth.ListUserDocuments(
    //        _firebaseStore.Data.Uid,
    //        _firebaseStore.Data.IdToken,
    //        _userId
    //    );

    //    foreach (FirestoreDocument document in list)
    //    {
    //        if (
    //            string.IsNullOrEmpty(document.Fields?.DocPath?.StringValue)
    //            || string.IsNullOrEmpty(document.Fields.Content?.StringValue)
    //        )
    //            continue;

    //        _secureBoxStore.CreateFile(
    //            document.Fields.DocPath.StringValue,
    //            document.Fields.Content.BytesValue
    //                ?? Encoding.UTF8.GetBytes(document.Fields.Content.StringValue)
    //        );
    //    }
    //}
}
