using SecureBox.Config;
using SecureBox.Stores;

namespace SecureBox.Services.Cloud;

/// <summary>
/// Outcome of a cloud operation
/// </summary>
/// <param name="Uploaded">Files sent to the cloud</param>
/// <param name="Downloaded">Files written to the vault</param>
/// <param name="Failed">Files that could not be sent or written</param>
public sealed record CloudSyncResult(int Uploaded, int Downloaded, int Failed)
{
    public override string ToString()
    {
        List<string> parts = [];
        if (Uploaded > 0)
            parts.Add($"{Uploaded} uploaded");
        if (Downloaded > 0)
            parts.Add($"{Downloaded} downloaded");
        if (Failed > 0)
            parts.Add($"{Failed} failed");

        return parts.Count == 0 ? "Everything is up to date." : string.Join(", ", parts) + ".";
    }
}

/// <summary>
/// Copies the encrypted files of the logged user between the vault and the
/// cloud. Files are never decrypted, the cloud only receives what is on disk.
/// </summary>
public sealed class CloudSyncService(
    ICloudService cloud,
    SecureBoxStore secureBoxStore,
    LoggedUserStore loggedUserStore
)
{
    public async Task<CloudSyncResult> UploadAsync(CancellationToken cancellationToken = default)
    {
        string userId = UserId();
        string userFolder = Path.Combine(VaultConfig.Instance.Vault, userId);
        if (!Directory.Exists(userFolder))
            return new(0, 0, 0);

        int uploaded = 0,
            failed = 0;

        foreach (string file in Directory.EnumerateFiles(userFolder, "*", SearchOption.AllDirectories))
        {
            byte[] content;
            try
            {
                content = await File.ReadAllBytesAsync(file, cancellationToken);
            }
            catch (IOException)
            {
                failed++;
                continue;
            }

            // Files the service cannot store are skipped, any other failure of
            // the service stops the upload and is reported to the user
            if (content.Length > FirebaseCloudService.MaxContentSize)
            {
                failed++;
                continue;
            }

            string path = CloudPaths.ToCloudPath(VaultConfig.Instance.Vault, file);
            await cloud.UploadAsync(userId, new CloudDocument(path, content), cancellationToken);
            uploaded++;
        }

        return new(uploaded, 0, failed);
    }

    public async Task<CloudSyncResult> DownloadAsync(CancellationToken cancellationToken = default)
    {
        string userId = UserId();
        var documents = await cloud.ListAsync(userId, cancellationToken);

        int downloaded = 0,
            failed = 0;

        foreach (var document in documents)
        {
            string? relative = CloudPaths.ToUserRelativePath(document.Path, userId);
            if (relative is null)
            {
                failed++;
                continue;
            }

            try
            {
                if (secureBoxStore.WriteFile(relative, document.Content))
                    downloaded++;
            }
            catch (IOException)
            {
                failed++;
            }
        }

        if (downloaded > 0)
            secureBoxStore.Reload();

        return new(0, downloaded, failed);
    }

    public async Task<CloudSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var upload = await UploadAsync(cancellationToken);
        var download = await DownloadAsync(cancellationToken);

        return new(upload.Uploaded, download.Downloaded, upload.Failed + download.Failed);
    }

    private string UserId() =>
        loggedUserStore.UserId ?? throw new CloudException("Log in to the vault first.");
}
