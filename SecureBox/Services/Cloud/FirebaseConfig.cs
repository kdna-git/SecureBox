using System.Reflection;
using System.Text.Json;

namespace SecureBox.Services.Cloud;

/// <summary>
/// Settings of the Firebase project used for the cloud copy of the vault.
/// They are read from <c>firebase.json</c>, which is embedded in the assembly at
/// build time and kept out of source control. Copy <c>firebase.example.json</c>
/// to <c>firebase.json</c> and fill it in to enable the cloud. Without it the
/// application works normally and the cloud reports that it is not configured.
/// </summary>
internal sealed class FirebaseConfig
{
    // Logical name given to the embedded file in SecureBox.csproj
    private const string ResourceName = "SecureBox.firebase.json";

    private static readonly Lazy<FirebaseConfig> _instance = new(Load);

    public static FirebaseConfig Instance => _instance.Value;

    public string ApiKey { get; init; } = string.Empty;
    public string AuthDomain { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(AuthDomain)
        && !string.IsNullOrWhiteSpace(ProjectId);

    private static FirebaseConfig Load()
    {
        try
        {
            using Stream? stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(ResourceName);

            if (stream is not null)
                return JsonSerializer.Deserialize<FirebaseConfig>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new FirebaseConfig();
        }
        catch (JsonException) { }
        return new FirebaseConfig();
    }
}
