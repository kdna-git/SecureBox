using FluentAssertions;
using SecureBox.Config;
using Xunit;

namespace SecureBox.Test;

public class StoragePathsTests
{
    [Fact]
    public void Resolve_StopsAtTheFirstFullyQualifiedFolder()
    {
        var probed = new List<Environment.SpecialFolder>();
        string local = Path.Combine(Path.GetTempPath(), "local");

        string result = StoragePaths.Resolve(
            folder =>
            {
                probed.Add(folder);
                return folder == Environment.SpecialFolder.LocalApplicationData ? local : "";
            },
            Path.GetTempPath()
        );

        result.Should().Be(Path.GetFullPath(local));
        probed.Should().ContainSingle();
    }

    [Fact]
    public void Resolve_PrefersLocalApplicationDataOverApplicationData()
    {
        string local = Path.Combine(Path.GetTempPath(), "local");
        string roaming = Path.Combine(Path.GetTempPath(), "roaming");

        string result = StoragePaths.Resolve(
            folder =>
                folder switch
                {
                    Environment.SpecialFolder.LocalApplicationData => local,
                    Environment.SpecialFolder.ApplicationData => roaming,
                    _ => "",
                },
            Path.GetTempPath()
        );

        result.Should().Be(Path.GetFullPath(local));
    }

    [Fact]
    public void Resolve_SkipsUnqualifiedFoldersAndFallsBackToTemp()
    {
        string temp = Path.Combine(Path.GetTempPath(), "fallback");

        string result = StoragePaths.Resolve(_ => "relative/not/qualified", temp);

        result.Should().Be(Path.GetFullPath(temp));
    }

    [Fact]
    public void Resolve_SkipsBlankFolders()
    {
        string temp = Path.Combine(Path.GetTempPath(), "fallback");

        string result = StoragePaths.Resolve(_ => "   ", temp);

        result.Should().Be(Path.GetFullPath(temp));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/path")]
    [InlineData("./relative")]
    public void SetAppDataRoot_IgnoresUnqualifiedPaths(string value)
    {
        string before = StoragePaths.AppDataRoot;

        StoragePaths.SetAppDataRoot(value);

        StoragePaths.AppDataRoot.Should().Be(before);
    }

    [Fact]
    public void AppDataRoot_IsAlwaysFullyQualified()
    {
        Path.IsPathFullyQualified(StoragePaths.AppDataRoot).Should().BeTrue();
    }
}
