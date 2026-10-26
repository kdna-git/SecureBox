using FluentAssertions;
using SecureBox.Config;
using Xunit;

namespace SecureBox.Test;

public class VaultConfigTests : IDisposable
{
    // The instance is a process wide singleton, so the original value is put
    // back to keep the other tests independent of this one
    private readonly string _original = VaultConfig.Instance.VaultPath;

    public void Dispose() => VaultConfig.Instance.VaultPath = _original;

    [Fact]
    public void Vault_IsAlwaysFullyQualified()
    {
        Path.IsPathFullyQualified(VaultConfig.Instance.Vault).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SecureBox")]
    [InlineData("relative/path")]
    [InlineData("./relative")]
    public void VaultPath_FallsBackToTheAppDataRoot_WhenGivenAnUnusableValue(string value)
    {
        VaultConfig.Instance.VaultPath = value;

        VaultConfig.Instance.VaultPath.Should().Be(StoragePaths.AppDataRoot);
        VaultConfig.Instance.Vault.Should().Be(
            Path.Combine(StoragePaths.AppDataRoot, "SecureBox")
        );
    }

    [Fact]
    public void VaultPath_AcceptsAFullyQualifiedValue()
    {
        string absolute = Path.Combine(Path.GetTempPath(), "MyVault");

        VaultConfig.Instance.VaultPath = absolute;

        VaultConfig.Instance.VaultPath.Should().Be(Path.GetFullPath(absolute));
    }

    [Fact]
    public void Vault_CombinesTheRootWithTheVaultName()
    {
        using var vault = new TempVault();
        VaultConfig.Instance.VaultPath = vault.Root;

        VaultConfig.Instance.Vault.Should().Be(Path.Combine(vault.Root, "SecureBox"));
    }
}
