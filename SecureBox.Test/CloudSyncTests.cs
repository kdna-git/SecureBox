using FluentAssertions;
using SecureBox.Config;
using SecureBox.Services.Cloud;
using SecureBox.Stores;
using Xunit;

namespace SecureBox.Test;

public class CloudPathsTests
{
    [Fact]
    public void ToCloudPath_IsRelativeToTheVaultWithForwardSlashes()
    {
        string vault = Path.Combine(Path.GetTempPath(), "vault");
        string file = Path.Combine(vault, "alice", "Banking", "Bank.kdna");

        CloudPaths.ToCloudPath(vault, file).Should().Be("alice/Banking/Bank.kdna");
    }

    [Theory]
    [InlineData("alice/Banking/Bank.kdna")]
    // Format written by the WPF version
    [InlineData("\\alice\\Banking\\Bank.kdna")]
    public void ToUserRelativePath_StripsTheUserFolder(string cloudPath)
    {
        CloudPaths.ToUserRelativePath(cloudPath, "alice")
            .Should()
            .Be(Path.Combine("Banking", "Bank.kdna"));
    }

    [Theory]
    [InlineData("bob/Bank.kdna")]
    [InlineData("alice")]
    [InlineData("alice/../bob/Bank.kdna")]
    [InlineData("alice/./Bank.kdna")]
    [InlineData("../alice/Bank.kdna")]
    public void ToUserRelativePath_RejectsPathsOutsideTheUserFolder(string cloudPath)
    {
        CloudPaths.ToUserRelativePath(cloudPath, "alice").Should().BeNull();
    }

    [Fact]
    public void DocumentId_DiffersForFilesWithTheSameNameInDifferentFolders()
    {
        string a = CloudPaths.DocumentId("alice/Banking/Bank.kdna");
        string b = CloudPaths.DocumentId("alice/Work/Bank.kdna");

        a.Should().NotBe(b);
        a.Should().MatchRegex("^[0-9a-f]{64}$");
        CloudPaths.DocumentId("alice/Banking/Bank.kdna").Should().Be(a);
    }
}

public class CloudSyncServiceTests : IDisposable
{
    private readonly TempVault _vault = new();
    private readonly string _original = VaultConfig.Instance.VaultPath;

    public void Dispose()
    {
        VaultConfig.Instance.VaultPath = _original;
        _vault.Dispose();
    }

    private sealed class FakeCloud : ICloudService
    {
        public Dictionary<string, List<CloudDocument>> Collections { get; } = [];

        public bool IsSignedIn => true;
        public string? Email => "someone@example.com";

        public event Action? SignedInChanged
        {
            add { }
            remove { }
        }

        public Task SignInAsync(string email, string password) => Task.CompletedTask;

        public Task SignUpAsync(string email, string password) => Task.CompletedTask;

        public void SignOut() { }

        public Task UploadAsync(string collection, CloudDocument document, CancellationToken cancellationToken = default)
        {
            var documents = Collections.TryGetValue(collection, out var list) ? list : Collections[collection] = [];
            documents.RemoveAll(d => d.Path == document.Path);
            documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CloudDocument>> ListAsync(string collection, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CloudDocument>>(Collections.GetValueOrDefault(collection) ?? []);
    }

    private async Task<(CloudSyncService Sync, SecureBoxStore Store, string UserFolder)> Setup(FakeCloud cloud)
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var user = new LoggedUserStore();
        var store = new SecureBoxStore(user);
        await user.SetUserAsync("alice");
        return (new CloudSyncService(cloud, store, user), store, Path.Combine(_vault.Root, "SecureBox", "alice"));
    }

    [Fact]
    public async Task Upload_SendsEveryFileOfTheUserWithItsVaultPath()
    {
        var cloud = new FakeCloud();
        var (sync, store, _) = await Setup(cloud);
        store.WriteFile("Email.kdna", [1]);
        store.WriteFile("Banking/Bank.kdna", [2]);

        var result = await sync.UploadAsync(TestContext.Current.CancellationToken);

        result.Uploaded.Should().Be(2);
        cloud.Collections["alice"].Select(d => d.Path)
            .Should()
            .BeEquivalentTo("alice/Email.kdna", "alice/Banking/Bank.kdna");
    }

    [Fact]
    public async Task Download_WritesTheFilesAndRefreshesTheStore()
    {
        var cloud = new FakeCloud();
        cloud.Collections["alice"] =
        [
            new("alice/Work/VPN.kdna", [7, 7]),
            new("\\alice\\Email.kdna", [8]),
        ];
        var (sync, store, userFolder) = await Setup(cloud);

        var result = await sync.DownloadAsync(TestContext.Current.CancellationToken);

        result.Downloaded.Should().Be(2);
        File.ReadAllBytes(Path.Combine(userFolder, "Work", "VPN.kdna")).Should().Equal(7, 7);
        store.Files.Select(f => f.Name).Should().BeEquivalentTo("Work", "Email");
    }

    [Fact]
    public async Task Download_IgnoresDocumentsThatWouldLeaveTheUserFolder()
    {
        var cloud = new FakeCloud();
        cloud.Collections["alice"] =
        [
            new("alice/../bob/Email.kdna", [1]),
            new("bob/Email.kdna", [1]),
        ];
        var (sync, _, _) = await Setup(cloud);

        var result = await sync.DownloadAsync(TestContext.Current.CancellationToken);

        result.Downloaded.Should().Be(0);
        result.Failed.Should().Be(2);
        Directory.GetFiles(_vault.Root, "*.kdna", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Sync_UploadsThenDownloadsWithoutDuplicatingFiles()
    {
        var cloud = new FakeCloud();
        cloud.Collections["alice"] = [new("alice/Remote.kdna", [5])];
        var (sync, store, userFolder) = await Setup(cloud);
        store.WriteFile("Local.kdna", [4]);

        var result = await sync.SyncAsync(TestContext.Current.CancellationToken);

        result.Should().Be(new CloudSyncResult(1, 1, 0));
        Directory.GetFiles(userFolder).Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo("Local.kdna", "Remote.kdna");

        // A second sync finds everything in place
        (await sync.SyncAsync(TestContext.Current.CancellationToken)).Downloaded.Should().Be(0);
    }

    [Fact]
    public void Result_DescribesWhatHappened()
    {
        new CloudSyncResult(0, 0, 0).ToString().Should().Be("Everything is up to date.");
        new CloudSyncResult(2, 1, 1).ToString().Should().Be("2 uploaded, 1 downloaded, 1 failed.");
    }
}
