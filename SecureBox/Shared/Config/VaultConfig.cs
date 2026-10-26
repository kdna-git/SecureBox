using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecureBox.Config;

public enum DataViewMode
{
    TreeView,
    MosaicView,
}

public sealed class VaultConfig
{
    #region Singleton Implementation

    /// <summary>
    /// Instancia única de la clase VaultConfig.
    /// </summary>
    private static readonly Lazy<VaultConfig> _instance = new Lazy<VaultConfig>(() => LoadConfig());

    /// <summary>
    /// Obtiene la instancia única de la clase VaultConfig.
    /// </summary>
    public static VaultConfig Instance => _instance.Value;

    /// <summary>
    /// Constructor privado para evitar la instanciación externa.
    /// </summary>
    [JsonConstructor]
    private VaultConfig() { }

    #endregion

    #region Public Fields and Properties

    // Resolved default, replaced when the user picks a different location
    public string VaultPath
    {
        get;
        set => field = IsAbsolute(value) ? Path.GetFullPath(value) : StoragePaths.AppDataRoot;
    } = StoragePaths.AppDataRoot;

    public string VaultName { get; } = "SecureBox";
    public string? VaultBookmark { get; set; }
    public string Vault => Path.Combine(VaultPath, VaultName);
    public DataViewMode DataView { get; set; }
    public int MosaicColumns { get; set; } = 6;
    public bool IsDarkMode { get; set; } = true;

    #endregion

    #region Private Fields and Properties

    private const string ConfigFolderName = "SecureVault";
    private const string FileName = "config.json";

    // Resolved on every access, so that the root supplied by the application
    // head is always taken into account instead of being captured too early
    private static string JsonPath => Path.Combine(StoragePaths.AppDataRoot, ConfigFolderName);
    private static string JsonFilePath => Path.Combine(JsonPath, FileName);

    /// <summary>
    /// Indicates whether the given path is usable as a vault location. Relative
    /// paths are rejected, because the process working directory is not a
    /// guaranteed writable location on every platform.
    /// </summary>
    private static bool IsAbsolute(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);

    #endregion

    #region Configuration Methods

    private static VaultConfig LoadConfig()
    {
        try
        {
            string jsonString = File.ReadAllText(JsonFilePath);
            return JsonSerializer.Deserialize<VaultConfig>(jsonString) ?? new VaultConfig();
        }
        catch (Exception)
        {
            return new VaultConfig();
        }
    }

    public void SaveConfig()
    {
        try
        {
            if (!Directory.Exists(JsonPath))
            {
                Directory.CreateDirectory(JsonPath);
            }
            string jsonString = JsonSerializer.Serialize(this);
            File.WriteAllText(JsonFilePath, jsonString);
        }
        catch (Exception) { }
    }

    #endregion
}
