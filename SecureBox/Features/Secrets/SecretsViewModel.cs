using MVVM.Core.Services;
using MVVM.Core.ViewModels;
using SecureBox.Services.Cloud;
using SecureBox.Stores;

namespace SecureBox.ViewModels;

public sealed class SecretsViewModel : ItemsViewModel
{
    public SecretsViewModel(
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
        FavoriteView = false;

        Filter = string.Empty;
        Refresh();

        secureBoxStore.FilesChanged += SecureBoxStore_FilesChanged;
        ObserveCloud();
    }

    public override void Dispose()
    {
        _secureBoxStore.FilesChanged -= SecureBoxStore_FilesChanged;

        base.Dispose();
    }

    private void SecureBoxStore_FilesChanged() => Refresh();

    protected override void LoadRoot()
    {
        AllItems = [.. _secureBoxStore.Files];
        Directories = [.. _secureBoxStore.Files.Where(d => d.Type == "Folder")];
        Files = [.. _secureBoxStore.Files.Where(d => d.Type == "File")];
    }
}
