# SecureBox — technical notes

Developer reference for the codebase. For how to use the app, see the [README](README.md).

The app is built with [Avalonia](https://avaloniaui.net/) 12 on .NET 10, with one shared codebase
and a thin head per platform: Windows/macOS/Linux, Android, iOS and the browser (WebAssembly). It
is a port of an earlier WPF version and reads the vaults that version wrote.

## Building and running

```bash
dotnet run --project SecureBox.Desktop
```

```bash
dotnet run --project SecureBox.Browser
```

```bash
dotnet build SecureBox.Android -t:Install
```

```bash
dotnet test
```

Tests run on Microsoft.Testing.Platform (`global.json`, `dotnet.config`). Package versions are
managed centrally in `Directory.Packages.props`; shared build properties live in
`Directory.Build.props`.

## Project layout

| Project | What it is |
|---|---|
| `SecureBox.Domain` | Models (`FileItem`). No UI, no storage. |
| `MVVM.Core` | Shell plumbing: `ViewModelBase`, navigation stores and services, the main window and shell view. |
| `ModalControl` | The modal overlay control. |
| `SecureBox` | The app: features, stores, services, encryption, theme and shared components. |
| `SecureBox.Desktop` / `.Android` / `.iOS` / `.Browser` | Platform heads. |
| `SecureBox.Test` | Unit tests: authentication, encryption, cloud sync, stores, paths, config. |
| `SecureBox.UiTest` | UI tests on Avalonia Headless with the Skia renderer. |
| `SecureBox.EntityFramework` | Template leftover; not in the solution and not referenced. |

Inside `SecureBox`:

| Folder | Contents |
|---|---|
| `Features/` | One folder per screen (view + view model): Login, SignUp, Secrets, Favorites, CreateSecret, OpenSecret, Settings, Layout. |
| `Stores/` | `LoggedUserStore` (user, key, favorites) and `SecureBoxStore` (file tree of the logged user). |
| `Services/` | Clipboard, file picker, theme and `Cloud/`. |
| `Shared/Methods/` | `Authentication` and `Encryption`. |
| `Shared/Config/` | `VaultConfig` and `StoragePaths`. |
| `Shared/Components/` | TreeView, MosaicView, BreadCrumb, ControlBar, FileIcon. |
| `DI/` | Service registration (`AddApplicationServices`) and `TopLevelProvider`. |

## Storage

`StoragePaths.AppDataRoot` resolves to the first fully qualified folder among
`LocalApplicationData`, `ApplicationData` and `UserProfile`, falling back to the temp folder. On
Android the head calls `StoragePaths.SetAppDataRoot(FilesDir)` in `Application.OnCreate`, before
Avalonia starts.

| What | Path |
|---|---|
| App config | `<AppDataRoot>/SecureVault/config.json` |
| Vault | `<VaultPath>/SecureBox/`, where `VaultPath` defaults to `AppDataRoot` and can be changed in Settings |
| User credentials | `<Vault>/<userId>.udata` (JSON: salt and PBKDF2 hash) |
| Favorites | `<Vault>/<userId>.fav` (JSON list of absolute paths) |
| Secrets | `<Vault>/<userId>/**` (text secrets use the `.kdna` extension) |

## Authentication and keys

- Passwords are handled as `byte[]` and zeroed once used.
- PBKDF2-SHA256 with a 32-byte random salt. The stored hash uses 1,000,000 iterations; the
  encryption key returned to the session uses 999,999, so the key is never the persisted value.
- `Authentication.IsValidUserId` rejects invalid file-name characters, both path separators and
  `.`/`..` on every platform, because the user ID becomes a file and folder name.
- The key lives in `LoggedUserStore.Key` and is zeroed on logout.

## File format

Every vault item, text or file, has the same layout (little endian, shared with the WPF version):

```
[int32 metadata length][metadata JSON]
[byte version = 1][int64 plaintext length]
repeated: [byte nonce length][nonce][16-byte tag][int32 ciphertext length][ciphertext]
```

- The metadata (`SecretMetadata`: original file name, URL, user, other, compress flag, dates) is
  stored **in clear text**, so it can be listed without the key. Do not put secrets there.
- Content is split into 1 MiB chunks, each encrypted with AES-GCM under its own random 12-byte
  nonce.
- With `Compress`, the content is GZipped before encryption.
- A failed write deletes the partial file. Reads cap the metadata at 1 MiB and validate every chunk
  length.

## Cloud

`ICloudService` is implemented by `FirebaseCloudService`: Firebase email/password auth plus the
Firestore REST API.

- Documents live at `users/{firebase uid}/{vault user id}/{document id}`, where the document ID is
  the SHA-256 of the cloud path, so files with the same name in different folders do not collide.
- Each document stores `docPath` (path relative to the vault, `/` separators) and `content`
  (the encrypted bytes, as `bytesValue`). Content from the WPF version may come as `stringValue`.
- Firestore documents are limited to 1 MiB, so content over 700 KB is skipped.
- `CloudSyncService` uploads every file of the user folder and downloads into it; nothing is ever
  decrypted or deleted. `CloudPaths.ToUserRelativePath` rejects paths of another user, `.`/`..`
  and invalid characters, and `SecureBoxStore.WriteFile` refuses anything outside the user folder.
- Firestore security rules must restrict `users/{uid}` to that user. A Firebase web API key is
  meant to ship with the client, so the rules (and the key's API restrictions in Google Cloud) are
  what protect the data, not the key being hidden.

### Firebase settings

The Firebase project is not in source control. `FirebaseConfig` reads `SecureBox/firebase.json`,
which `SecureBox.csproj` embeds into the assembly when the file exists, so it reaches every head
(desktop, Android, iOS, browser) without a runtime file. It is listed in `.gitignore`.

To set it up, copy the template and fill in the values from Firebase console → Project settings →
General:

```bash
cp SecureBox/firebase.example.json SecureBox/firebase.json
```

Without the file the app builds and runs normally; signing in to the cloud shows "The cloud is not
configured in this build". To rotate the key, create a new one in Google Cloud Console → APIs &
Services → Credentials, put it in `firebase.json`, rebuild, then delete the old key.

## Navigation and DI

- `App.OnFrameworkInitializationCompleted` builds the `ServiceCollection`, applies the saved theme
  and navigates to Login.
- Desktop uses `MainWindow`; Android/iOS/browser use `ShellView` (no header).
- `NavigationStore` keeps a back stack, `ModalNavigationStore` shows a single modal (create and
  open secret). On Android the back button closes the modal first, then goes back, then exits.
- `ParameterNavigationService<TParam, TViewModel>` builds a view model from a parameter, e.g. an
  `OpenSecretViewModel` from a `FileItemViewModel`.
- `TopLevelProvider` gives services (clipboard, file picker) the current `TopLevel` on every
  lifetime.

## Pitfalls

- **`Path.Combine` discards the vault when the second argument is rooted.** Every user ID and cloud
  path is validated before it reaches the file system; keep it that way.
- **`VaultConfig.Instance` is lazy and read once.** `StoragePaths.SetAppDataRoot` (Android, UI
  tests) must run before it is first touched.
- **Favorites and tree items are matched by path**, not by instance: a folder opened in a view is
  reloaded and its items are new objects.
- **Deletion uses the Recycle Bin only on Windows** (`Microsoft.VisualBasic.FileIO`); elsewhere it
  is permanent.
- **The file format is shared with the WPF version.** Any change needs a new format version byte.
- **Developer tools attach once per process**, because the UI tests create a new `App` per test.
