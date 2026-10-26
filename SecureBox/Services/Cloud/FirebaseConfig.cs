namespace SecureBox.Services.Cloud;

/// <summary>
/// Settings of the Firebase project used for the cloud copy of the vault.
/// The API key of a Firebase web app identifies the project and is meant to be
/// shipped with the client, access to the data is enforced by the security rules.
/// </summary>
internal static class FirebaseConfig
{
    public const string ApiKey = "REDACTED-FIREBASE-API-KEY";
    public const string AuthDomain = "box-f3ad8.firebaseapp.com";
    public const string ProjectId = "box-f3ad8";
}
