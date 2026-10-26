using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Firebase.Auth;
using Firebase.Auth.Providers;

namespace SecureBox.Services.Cloud;

/// <summary>
/// An encrypted vault file stored in the cloud
/// </summary>
/// <param name="Path">The path of the file relative to the vault, using '/' separators</param>
/// <param name="Content">The encrypted content of the file</param>
public sealed record CloudDocument(string Path, byte[] Content);

/// <summary>
/// A failure of the cloud service, with a message that can be shown to the user
/// </summary>
public sealed class CloudException(string message, Exception? inner = null)
    : Exception(message, inner);

public interface ICloudService
{
    bool IsSignedIn { get; }
    string? Email { get; }

    event Action? SignedInChanged;

    Task SignInAsync(string email, string password);
    Task SignUpAsync(string email, string password);
    void SignOut();

    Task UploadAsync(string collection, CloudDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CloudDocument>> ListAsync(string collection, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores the vault files of the users in Cloud Firestore, signing in with
/// Firebase email and password accounts. Documents live in
/// <c>users/{firebase uid}/{collection}/{document id}</c>.
/// </summary>
public sealed class FirebaseCloudService : ICloudService
{
    // Firestore rejects documents larger than 1 MiB, and the content is sent as base64
    public const int MaxContentSize = 700 * 1024;

    private const int PageSize = 300;

    private static readonly HttpClient Http = new();

    private readonly FirebaseAuthClient _auth = new(
        new FirebaseAuthConfig
        {
            ApiKey = FirebaseConfig.ApiKey,
            AuthDomain = FirebaseConfig.AuthDomain,
            Providers = [new EmailProvider()],
        }
    );

    private User? _user;

    public bool IsSignedIn => _user is not null;
    public string? Email => _user?.Info.Email;

    public event Action? SignedInChanged;

    #region Authentication

    public async Task SignInAsync(string email, string password)
    {
        var credential = await Authenticate(() =>
            _auth.SignInWithEmailAndPasswordAsync(email, password)
        );
        SetUser(credential.User);
    }

    public async Task SignUpAsync(string email, string password)
    {
        var credential = await Authenticate(() =>
            _auth.CreateUserWithEmailAndPasswordAsync(email, password)
        );
        SetUser(credential.User);
    }

    public void SignOut()
    {
        _auth.SignOut();
        SetUser(null);
    }

    private void SetUser(User? user)
    {
        _user = user;
        SignedInChanged?.Invoke();
    }

    private static async Task<UserCredential> Authenticate(Func<Task<UserCredential>> request)
    {
        try
        {
            return await request();
        }
        catch (FirebaseAuthException ex)
        {
            throw new CloudException(Describe(ex.Reason), ex);
        }
        catch (HttpRequestException ex)
        {
            throw new CloudException("The cloud service could not be reached.", ex);
        }
    }

    private static string Describe(AuthErrorReason reason) =>
        reason switch
        {
            AuthErrorReason.WrongPassword
            or AuthErrorReason.UnknownEmailAddress
            or AuthErrorReason.UserNotFound => "Invalid email or password.",
            AuthErrorReason.EmailExists => "An account with this email already exists.",
            AuthErrorReason.WeakPassword => "The password must have at least 6 characters.",
            AuthErrorReason.InvalidEmailAddress or AuthErrorReason.MissingEmail =>
                "The email address is not valid.",
            AuthErrorReason.MissingPassword => "The password is required.",
            AuthErrorReason.UserDisabled => "This account has been disabled.",
            AuthErrorReason.TooManyAttemptsTryLater => "Too many attempts, try again later.",
            _ => $"The request was rejected ({reason}).",
        };

    #endregion

    #region Firestore

    public async Task UploadAsync(
        string collection,
        CloudDocument document,
        CancellationToken cancellationToken = default
    )
    {
        if (document.Content.Length > MaxContentSize)
            throw new CloudException($"{document.Path} is too large for the cloud.");

        string url = $"{CollectionUrl(collection)}/{CloudPaths.DocumentId(document.Path)}";
        var body = new FirestoreDocument
        {
            Fields = new FirestoreFields
            {
                Content = new FirestoreValue { BytesValue = document.Content },
                DocPath = new FirestoreValue { StringValue = document.Path },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body),
        };
        using var response = await Send(request, cancellationToken);
    }

    public async Task<IReadOnlyList<CloudDocument>> ListAsync(
        string collection,
        CancellationToken cancellationToken = default
    )
    {
        List<CloudDocument> documents = [];
        string? pageToken = null;

        do
        {
            string url = $"{CollectionUrl(collection)}?pageSize={PageSize}";
            if (pageToken is not null)
                url += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await Send(request, cancellationToken);

            var page = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(
                cancellationToken
            );

            foreach (var document in page?.Documents ?? [])
            {
                string? path = document.Fields?.DocPath?.StringValue;
                // Files uploaded by the WPF version may carry the content as text
                byte[]? content =
                    document.Fields?.Content?.BytesValue
                    ?? (
                        document.Fields?.Content?.StringValue is { } text
                            ? System.Text.Encoding.UTF8.GetBytes(text)
                            : null
                    );

                if (!string.IsNullOrEmpty(path) && content is not null)
                    documents.Add(new CloudDocument(path, content));
            }

            pageToken = page?.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return documents;
    }

    private string CollectionUrl(string collection)
    {
        string uid = _user?.Uid ?? throw new CloudException("Sign in to the cloud first.");

        return $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}"
            + $"/databases/(default)/documents/users/{Uri.EscapeDataString(uid)}"
            + $"/{Uri.EscapeDataString(collection)}";
    }

    private async Task<HttpResponseMessage> Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (_user is null)
            throw new CloudException("Sign in to the cloud first.");

        HttpResponseMessage response;
        try
        {
            // The token is refreshed by the client when it is about to expire
            string token = await _user.GetIdTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await Http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new CloudException("The cloud service could not be reached.", ex);
        }
        catch (FirebaseAuthException ex)
        {
            throw new CloudException(Describe(ex.Reason), ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new CloudException($"The cloud service answered {(int)status} ({status}).");
        }

        return response;
    }

    #endregion

    #region Firestore REST model

    private sealed class FirestoreListResponse
    {
        [JsonPropertyName("documents")]
        public List<FirestoreDocument>? Documents { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }
    }

    private sealed class FirestoreDocument
    {
        [JsonPropertyName("fields")]
        public FirestoreFields? Fields { get; set; }
    }

    private sealed class FirestoreFields
    {
        [JsonPropertyName("content")]
        public FirestoreValue? Content { get; set; }

        [JsonPropertyName("docPath")]
        public FirestoreValue? DocPath { get; set; }
    }

    private sealed class FirestoreValue
    {
        [JsonPropertyName("stringValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StringValue { get; set; }

        [JsonPropertyName("bytesValue")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public byte[]? BytesValue { get; set; }
    }

    #endregion
}
