namespace SecureBox.Test;

/// <summary>
/// Temporary directory used as a vault root for the duration of a single test.
/// The directory is not created up front, so tests that assert on the creation
/// behaviour of the code under test start from a missing path.
/// </summary>
internal sealed class TempVault : IDisposable
{
    public string Root { get; }

    public TempVault()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "SecureBoxTests",
            Guid.NewGuid().ToString("N")
        );
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
