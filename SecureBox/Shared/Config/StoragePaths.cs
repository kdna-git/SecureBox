namespace SecureBox.Config;

/// <summary>
/// Resolves the absolute root directory used to store the vault and the application
/// configuration. The resolved value is always a fully qualified path so that it can
/// be used as the base for <see cref="Directory"/> and <see cref="File"/> operations.
/// </summary>
public static class StoragePaths
{
    #region Private Fields and Properties

    // Platform specific root supplied by the application head on startup
    private static string? _override;

    // Root derived from the well known folders of the current platform
    private static readonly Lazy<string> _fallback = new(() =>
        Resolve(Environment.GetFolderPath, Path.GetTempPath())
    );

    #endregion

    #region Public Fields and Properties

    /// <summary>
    /// Gets the absolute root directory for application data. The platform specific
    /// root takes precedence over the value derived from the well known folders.
    /// </summary>
    public static string AppDataRoot => _override ?? _fallback.Value;

    #endregion

    #region Public Methods

    /// <summary>
    /// Overrides the root directory for application data. Application heads must call
    /// this before the vault is accessed when the default resolution is unsuitable for
    /// the platform, as is the case on Android where the application private directory
    /// is the only location that is guaranteed to be writable.
    /// </summary>
    /// <param name="path">The absolute path to use as the application data root</param>
    public static void SetAppDataRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            return;

        _override = Path.GetFullPath(path);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Resolves the first well known folder that yields a fully qualified path.
    /// The last candidate is used only when none of the folders resolve to an
    /// absolute path, which happens on platforms where none of them are populated.
    /// </summary>
    /// <param name="probe">Resolves a special folder to its path for the current platform</param>
    /// <param name="tempPath">The path of the temporary directory, used as the last candidate</param>
    /// <returns>The first fully qualified path, or the temporary directory when none is available</returns>
    internal static string Resolve(Func<Environment.SpecialFolder, string> probe, string tempPath)
    {
        Environment.SpecialFolder[] candidates =
        [
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.UserProfile,
        ];

        foreach (Environment.SpecialFolder candidate in candidates)
        {
            string path = probe(candidate);
            if (!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
                return Path.GetFullPath(path);
        }

        return Path.GetFullPath(tempPath);
    }

    #endregion
}
