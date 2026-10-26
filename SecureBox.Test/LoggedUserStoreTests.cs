using FluentAssertions;
using SecureBox.Config;
using SecureBox.Stores;
using Xunit;

namespace SecureBox.Test;

public class LoggedUserStoreTests : IDisposable
{
    private readonly TempVault _vault = new();
    private readonly string _original = VaultConfig.Instance.VaultPath;

    public void Dispose()
    {
        VaultConfig.Instance.VaultPath = _original;
        _vault.Dispose();
    }

    private string VaultDir => Path.Combine(_vault.Root, "SecureBox");

    [Fact]
    public async Task SetUserAsync_CreatesTheUserDirectoryInsideTheVault()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var store = new LoggedUserStore();

        await store.SetUserAsync("alice");

        store.UserId.Should().Be("alice");
        Directory.Exists(Path.Combine(VaultDir, "alice")).Should().BeTrue();
    }

    [Fact]
    public async Task SetUserAsync_DoesNotCreateADirectoryWhenTheVaultIsMissing()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var store = new LoggedUserStore();

        await store.SetUserAsync("alice");

        // A missing vault must not stop the user from being logged in
        store.IsLogged.Should().BeTrue();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task SetUserAsync_WithTraversalUserId_StaysInsideTheVault(string userId)
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        Directory.CreateDirectory(VaultDir);
        var store = new LoggedUserStore();

        await store.SetUserAsync(userId);

        // Path.Combine discards the vault when the second argument is rooted,
        // so nothing may be created inside the vault nor beside it
        Directory.GetFileSystemEntries(VaultDir).Should().BeEmpty();
        Directory.GetFileSystemEntries(_vault.Root).Should().Equal([VaultDir]);
    }

    [Fact]
    public async Task SetUserAsync_WithNull_LogsTheUserOut()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var store = new LoggedUserStore();
        await store.SetUserAsync("alice");

        await store.SetUserAsync(null);

        store.UserId.Should().BeNull();
        store.IsLogged.Should().BeFalse();
    }

    [Fact]
    public async Task SetUserAsync_LoadsTheFavoritesOfTheUser()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        Directory.CreateDirectory(VaultDir);
        await File.WriteAllTextAsync(
            Path.Combine(VaultDir, "alice.fav"),
            """["/first/path", "/second/path"]""",
            TestContext.Current.CancellationToken
        );
        var store = new LoggedUserStore();

        await store.SetUserAsync("alice");

        store.Favorites.Should().Equal("/first/path", "/second/path");
    }

    [Fact]
    public async Task SetUserAsync_WithTheSameUser_KeepsTheFavorites()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        Directory.CreateDirectory(VaultDir);
        await File.WriteAllTextAsync(
            Path.Combine(VaultDir, "alice.fav"),
            """["/first/path"]""",
            TestContext.Current.CancellationToken
        );
        var store = new LoggedUserStore();
        await store.SetUserAsync("alice");

        await store.SetUserAsync("alice");

        store.Favorites.Should().Equal("/first/path");
    }

    private async Task<(LoggedUserStore Store, string Folder, string File)> LoggedStoreWithItems()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var store = new LoggedUserStore();
        await store.SetUserAsync("alice");

        string folder = Path.Combine(VaultDir, "alice", "folder");
        string file = Path.Combine(folder, "secret.kdna");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(file, "x", TestContext.Current.CancellationToken);

        return (store, folder, file);
    }

    [Fact]
    public async Task AddToFavorites_IsPersistedForTheNextLogin()
    {
        var (store, _, file) = await LoggedStoreWithItems();

        store.AddToFavorites(file);
        store.Favorites.Should().Equal(file);

        await store.SetUserAsync(null);
        store.Favorites.Should().BeEmpty();

        await store.SetUserAsync("alice");
        store.Favorites.Should().Equal(file);
    }

    [Fact]
    public async Task AddToFavorites_IgnoresMissingPathsAndDuplicates()
    {
        var (store, _, file) = await LoggedStoreWithItems();

        store.AddToFavorites(file);
        store.AddToFavorites(file);
        store.AddToFavorites(Path.Combine(VaultDir, "missing"));

        store.Favorites.Should().Equal(file);
    }

    [Fact]
    public async Task RemoveFromFavorites_RemovesOnlyThatPath()
    {
        var (store, folder, file) = await LoggedStoreWithItems();
        string sibling = folder + "-sibling";
        Directory.CreateDirectory(sibling);
        store.AddToFavorites(folder);
        store.AddToFavorites(file);
        store.AddToFavorites(sibling);

        store.RemoveFromFavorites(folder);

        store.Favorites.Should().BeEquivalentTo([file, sibling]);
    }

    [Fact]
    public async Task RemoveFromFavorites_WithChildren_KeepsFoldersSharingThePrefix()
    {
        var (store, folder, file) = await LoggedStoreWithItems();
        string sibling = folder + "-sibling";
        Directory.CreateDirectory(sibling);
        store.AddToFavorites(folder);
        store.AddToFavorites(file);
        store.AddToFavorites(sibling);

        store.RemoveFromFavorites(folder, includeChildren: true);

        store.Favorites.Should().Equal(sibling);
    }

    [Fact]
    public async Task SetUserAsync_RaisesLoggedUserChangedWithTheNewFavoritesLoaded()
    {
        var (store, _, file) = await LoggedStoreWithItems();
        store.AddToFavorites(file);
        await store.SetUserAsync(null);

        List<string>? seen = null;
        store.LoggedUserChanged += () => seen = store.Favorites;
        await store.SetUserAsync("alice");

        seen.Should().Equal(file);
    }

    [Fact]
    public async Task SetUserAsync_WithNull_ClearsTheKey()
    {
        VaultConfig.Instance.VaultPath = _vault.Root;
        var store = new LoggedUserStore();
        byte[] key = [1, 2, 3];
        store.Key = key;
        await store.SetUserAsync("alice");

        await store.SetUserAsync(null);

        store.Key.Should().BeNull();
        key.Should().OnlyContain(b => b == 0);
    }
}
