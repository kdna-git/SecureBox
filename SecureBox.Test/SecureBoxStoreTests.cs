using FluentAssertions;
using SecureBox.Config;
using SecureBox.Stores;
using Xunit;

namespace SecureBox.Test;

public class SecureBoxStoreTests : IDisposable
{
    private readonly TempVault _vault = new();
    private readonly string _original = VaultConfig.Instance.VaultPath;

    public void Dispose()
    {
        VaultConfig.Instance.VaultPath = _original;
        _vault.Dispose();
    }

    private string UserFolder => Path.Combine(_vault.Root, "SecureBox", "alice");

    private async Task<SecureBoxStore> LoggedStore()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var user = new LoggedUserStore();
        var store = new SecureBoxStore(user);
        await user.SetUserAsync("alice");
        return store;
    }

    [Fact]
    public async Task WriteFile_CreatesTheFileAndItsFolders()
    {
        var store = await LoggedStore();

        store.WriteFile("Banking/Cards/Visa.kdna", [1, 2, 3]).Should().BeTrue();

        File.ReadAllBytes(Path.Combine(UserFolder, "Banking", "Cards", "Visa.kdna"))
            .Should()
            .Equal(1, 2, 3);
    }

    [Fact]
    public async Task WriteFile_WithTheSameContent_LeavesTheFileAlone()
    {
        var store = await LoggedStore();
        store.WriteFile("Email.kdna", [1, 2, 3]);

        store.WriteFile("Email.kdna", [1, 2, 3]).Should().BeFalse();

        Directory.GetFiles(UserFolder).Should().ContainSingle();
    }

    [Fact]
    public async Task WriteFile_WithDifferentContent_KeepsBothFiles()
    {
        var store = await LoggedStore();
        store.WriteFile("Email.kdna", [1, 2, 3]);

        store.WriteFile("Email.kdna", [4, 5, 6]).Should().BeTrue();

        File.ReadAllBytes(Path.Combine(UserFolder, "Email.kdna")).Should().Equal(1, 2, 3);
        File.ReadAllBytes(Path.Combine(UserFolder, "Email (1).kdna")).Should().Equal(4, 5, 6);
    }

    [Theory]
    [InlineData("../bob/Email.kdna")]
    [InlineData("../../escape.kdna")]
    [InlineData("folder/../../bob/Email.kdna")]
    public async Task WriteFile_OutsideTheUserFolder_WritesNothing(string path)
    {
        var store = await LoggedStore();
        Directory.CreateDirectory(UserFolder);

        store.WriteFile(path, [1]).Should().BeFalse();

        Directory.GetFiles(_vault.Root, "*.kdna", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Reload_ShowsFilesWrittenToDisk()
    {
        var store = await LoggedStore();
        store.WriteFile("Work/VPN.kdna", [1]);

        bool notified = false;
        store.FilesChanged += () => notified = true;
        store.Reload();

        notified.Should().BeTrue();
        store.Files.Should().ContainSingle(f => f.Name == "Work")
            .Which.SubItems.Should().ContainSingle(f => f.Name == "VPN");
    }

    [Fact]
    public async Task RemoveItem_DeletesTheFileAndItsFavorite()
    {
        var store = await LoggedStore();
        store.WriteFile("Work/VPN.kdna", [1]);
        store.Reload();
        var vpn = store.Files.Single().SubItems.Single();
        vpn.IsFavorite = true;

        store.RemoveItem(vpn).Should().BeTrue();

        File.Exists(Path.Combine(UserFolder, "Work", "VPN.kdna")).Should().BeFalse();
        store.FavoriteFiles.Should().BeEmpty();
    }
}
