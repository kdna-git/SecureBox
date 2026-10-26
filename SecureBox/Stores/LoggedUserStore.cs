using System.Security.Cryptography;
using System.Text.Json;
using SecureBox.Config;
using SecureBox.Methods;

namespace SecureBox.Stores;

public sealed class LoggedUserStore
{
    #region Fields and Properties

    // The ID of the currently logged user.
    public string? UserId
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            LoggedUserChanged?.Invoke();
        }
    } = null;

    // Returns true if a user is logged in.
    public bool IsLogged => !string.IsNullOrEmpty(UserId);

    // Stores the encryption key associated with the logged user.
    public byte[]? Key { get; set; }

    // List of favorite file or directory paths for the current user.
    // The getter returns a snapshot, the list itself is only changed under the lock.
    private readonly Lock _lock = new();
    private List<string> _favorites = [];
    public List<string> Favorites
    {
        get
        {
            lock (_lock)
            {
                return [.. _favorites];
            }
        }
    }

    #endregion

    #region Events

    // Event triggered when the logged-in user changes.
    public event Action? LoggedUserChanged;

    // Event triggered when the list of favorites changes.
    public event Action? FavoritesChanged;

    #endregion

    #region Public Methods

    public async Task SetUserAsync(string? newUserId)
    {
        if (newUserId == UserId)
            return;

        if (newUserId is not null)
            TryCreateUserDirectory(newUserId);

        // Save old user's favorites
        if (!string.IsNullOrEmpty(UserId))
            SaveFavorites(UserId, Favorites);

        // The favorites are loaded before the user changes, so that the
        // subscribers of LoggedUserChanged already see the new user's favorites
        List<string> favorites = string.IsNullOrEmpty(newUserId)
            ? []
            : await Task.Run(() => LoadFavoritesAsync(newUserId));

        lock (_lock)
        {
            _favorites = favorites;
        }

        // The key belongs to the previous user and must not outlive the session
        if (newUserId is null && Key is not null)
        {
            CryptographicOperations.ZeroMemory(Key);
            Key = null;
        }

        UserId = newUserId;
        FavoritesChanged?.Invoke();
    }

    /// <summary>
    /// Adds a path to the user's favorites list if it exists and is not already included.
    /// </summary>
    /// <param name="path">The file or directory path to add.</param>
    public void AddToFavorites(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        // Check if the path exists and avoid adding duplicates
        if (!(File.Exists(path) || Directory.Exists(path)))
            return;

        lock (_lock)
        {
            if (_favorites.Contains(path))
                return;
            _favorites.Add(path);
        }

        FavoritesChanged?.Invoke();
        SaveFavorites(UserId, Favorites);
    }

    /// <summary>
    /// Removes a path from the user's favorites list if it exists.
    /// </summary>
    /// <param name="path">The file or directory path to remove.</param>
    /// <param name="includeChildren">
    /// Also removes the favorites located inside the path, used when a folder is deleted
    /// </param>
    public void RemoveFromFavorites(string path, bool includeChildren = false)
    {
        if (string.IsNullOrEmpty(path))
            return;

        string folderPrefix = Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar;

        lock (_lock)
        {
            int removed = _favorites.RemoveAll(fav =>
                fav == path || (includeChildren && fav.StartsWith(folderPrefix))
            );

            if (removed == 0)
                return;
        }

        FavoritesChanged?.Invoke();
        SaveFavorites(UserId, Favorites);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates the directory that holds the data of the given user.
    /// Failures are not propagated, so that a missing directory does not
    /// prevent the user from being logged in.
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    private static void TryCreateUserDirectory(string userId)
    {
        // Path.Combine discards the vault when the second argument is rooted,
        // so the user ID has to be rejected before it reaches the file system
        if (!Authentication.IsValidUserId(userId))
            return;

        try
        {
            string userPath = Path.Combine(VaultConfig.Instance.Vault, userId);
            if (!Directory.Exists(userPath))
                Directory.CreateDirectory(userPath);
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Conctructs the file path for the user's favorites list.
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>The path of the favorites file, or null if the user ID cannot be used as a file name</returns>
    private static string? GetFavoritesFilePath(string userId)
    {
        if (!Authentication.IsValidUserId(userId))
            return null;

        return Path.Combine(VaultConfig.Instance.Vault, $"{userId}.fav");
    }

    /// <summary>
    /// Loads the favorites list for the current user from a file.
    /// </summary>
    private static async Task<List<string>> LoadFavoritesAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return [];

        string? filePath = GetFavoritesFilePath(userId);

        if (string.IsNullOrEmpty(filePath))
            return [];

        // If the file does not exist, set an empty list
        if (!File.Exists(filePath))
            return [];

        try
        {
            // Deserialize JSON from the favorites file
            string json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception) { }
        return [];
    }

    /// <summary>
    /// Saves the current favorites list to a file for the logged-in user.
    /// </summary>
    private static void SaveFavorites(string? userId, List<string> favorites)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        string? filePath = GetFavoritesFilePath(userId);

        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            // Serialize favorites list to JSON and write to file. The file is
            // small, and writing it synchronously keeps consecutive saves in order
            string json = JsonSerializer.Serialize(favorites);
            File.WriteAllText(filePath, json);
        }
        catch (Exception) { }
    }

    #endregion
}
