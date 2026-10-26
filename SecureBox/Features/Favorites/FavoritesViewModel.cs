using MVVM.Core.Services;
using SecureBox.Services.Cloud;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public sealed class FavoritesViewModel : ItemsViewModel
{
    public FavoritesViewModel(
        SecureBoxStore secureBoxStore,
        string userId,
        INavigationService addSecretNavigationService,
        ParameterNavigationService<
            FileItemViewModel,
            OpenSecretViewModel
        > openFileItemNavigationService,
        ICloudService cloud,
        CloudSyncService cloudSync
    )
        : base(
            secureBoxStore,
            userId,
            addSecretNavigationService,
            openFileItemNavigationService,
            cloud,
            cloudSync
        )
    {
        FavoriteView = true;

        Filter = string.Empty;
        Refresh();

        secureBoxStore.FilesChanged += SecureBoxStore_Changed;
        secureBoxStore.FavoriteFilesChanged += SecureBoxStore_Changed;
        ObserveCloud();
    }

    public override void Dispose()
    {
        _secureBoxStore.FilesChanged -= SecureBoxStore_Changed;
        _secureBoxStore.FavoriteFilesChanged -= SecureBoxStore_Changed;

        base.Dispose();
    }

    private void SecureBoxStore_Changed() => Refresh();

    protected override string RootName => "Favorites";

    /// <summary>
    /// The tree shows every favorite that is not already shown inside a favorite
    /// folder, while the mosaic shows every favorite as a flat list.
    /// </summary>
    protected override void LoadRoot()
    {
        AllItems = [.. GetTopLevelFavorites(_secureBoxStore.Files)];
        Directories = [.. _secureBoxStore.FavoriteFiles.Where(d => d.Type == "Folder")];
        Files = [.. _secureBoxStore.FavoriteFiles.Where(d => d.Type == "File")];
    }

    private static IEnumerable<FileItemViewModel> GetTopLevelFavorites(
        IEnumerable<FileItemViewModel> items
    )
    {
        foreach (var item in items)
        {
            if (item.FavoriteDepth == 0)
            {
                yield return item;
                continue;
            }

            foreach (var child in GetTopLevelFavorites(item.SubItems))
                yield return child;
        }
    }
}
