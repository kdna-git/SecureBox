using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SecureBox.Methods;
using Xunit;

namespace SecureBox.Test;

public class AuthenticationTests
{
    // Mirrors Authentication.KeyIterations, which is one iteration less than the
    // count used for the stored hash
    private const int KeyIterations = 999_999;

    private static byte[] Password(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Derive(byte[] password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            KeyIterations,
            HashAlgorithmName.SHA256,
            32
        );

    private static byte[] ReadSalt(string userFile)
    {
        string json = File.ReadAllText(userFile);
        var data = JsonSerializer.Deserialize<UserFile>(json);
        return Convert.FromBase64String(data!.Salt);
    }

    private sealed record UserFile(string UserId, string Salt, string Hash);

    [Fact]
    public void SignUp_ThenLogin_ReturnsTheSameKey()
    {
        using var vault = new TempVault();

        byte[]? signUpKey = Authentication.SignUp(vault.Root, "alice", Password("pw"));
        byte[]? loginKey = Authentication.Login(vault.Root, "alice", Password("pw"));

        signUpKey.Should().NotBeNull();
        loginKey.Should().Equal(signUpKey!);
    }

    [Fact]
    public void SignUp_DerivesTheKeyFromThePasswordAndNotFromZeroedBytes()
    {
        using var vault = new TempVault();
        const string secret = "a very secret password";

        byte[]? key = Authentication.SignUp(vault.Root, "alice", Password(secret));

        key.Should().NotBeNull();

        byte[] salt = ReadSalt(Path.Combine(vault.Root, "alice.udata"));
        byte[] expected = Derive(Password(secret), salt);
        byte[] zeroDerived = Derive(new byte[32], salt);

        key.Should().Equal(expected);
        key.Should().NotEqual(zeroDerived);
    }

    [Fact]
    public void SignUp_ZeroesThePassword()
    {
        using var vault = new TempVault();
        byte[] password = Password("secret");

        Authentication.SignUp(vault.Root, "alice", password);

        password.Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void Login_ZeroesThePassword()
    {
        using var vault = new TempVault();
        Authentication.SignUp(vault.Root, "alice", Password("pw"));

        byte[] password = Password("pw");
        Authentication.Login(vault.Root, "alice", password);

        password.Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void Login_WithWrongPassword_ReturnsNull()
    {
        using var vault = new TempVault();
        Authentication.SignUp(vault.Root, "alice", Password("right")).Should().NotBeNull();

        Authentication.Login(vault.Root, "alice", Password("wrong")).Should().BeNull();
    }

    [Fact]
    public void Login_ForUnknownUser_ReturnsNull()
    {
        using var vault = new TempVault();
        Directory.CreateDirectory(vault.Root);

        Authentication.Login(vault.Root, "nobody", Password("pw")).Should().BeNull();
    }

    [Fact]
    public void SignUp_WithExistingUser_ReturnsTheSameKeyAsLogin()
    {
        using var vault = new TempVault();

        byte[]? first = Authentication.SignUp(vault.Root, "alice", Password("pw"));
        byte[]? again = Authentication.SignUp(vault.Root, "alice", Password("pw"));
        byte[]? login = Authentication.Login(vault.Root, "alice", Password("pw"));

        again.Should().Equal(first!);
        login.Should().Equal(first!);
    }

    [Fact]
    public void SignUp_UsesADistinctSaltPerUser()
    {
        using var vault = new TempVault();

        byte[]? alice = Authentication.SignUp(vault.Root, "alice", Password("same"));
        byte[]? bob = Authentication.SignUp(vault.Root, "bob", Password("same"));

        alice.Should().NotBeNull();
        bob.Should().NotBeNull();
        bob.Should().NotEqual(alice!);
    }

    [Fact]
    public void SignUp_CreatesTheVaultDirectoryAndTheUserFile()
    {
        using var vault = new TempVault();
        string nested = Path.Combine(vault.Root, "nested", "deeper");

        Authentication.SignUp(nested, "alice", Password("pw")).Should().NotBeNull();

        Directory.Exists(nested).Should().BeTrue();
        File.Exists(Path.Combine(nested, "alice.udata")).Should().BeTrue();
    }

    [Fact]
    public void SignUp_WithEmptyPassword_ReturnsNull()
    {
        using var vault = new TempVault();

        Authentication.SignUp(vault.Root, "alice", []).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SecureBox")]
    [InlineData("./relative")]
    public void SignUp_WithUnusablePath_ReturnsNull(string path)
    {
        Authentication.SignUp(path, "alice", Password("pw")).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SecureBox")]
    [InlineData("./relative")]
    public void Login_WithUnusablePath_ReturnsNull(string path)
    {
        Authentication.Login(path, "alice", Password("pw")).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SignUp_WithBlankUserId_ReturnsNull(string userId)
    {
        using var vault = new TempVault();

        Authentication.SignUp(vault.Root, userId, Password("pw")).Should().BeNull();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void SignUp_WithTraversalUserId_ReturnsNullAndWritesNothing(string userId)
    {
        using var vault = new TempVault();
        Directory.CreateDirectory(vault.Root);

        byte[]? key = Authentication.SignUp(vault.Root, userId, Password("pw"));

        key.Should().BeNull();
        Directory.GetFileSystemEntries(vault.Root).Should().BeEmpty();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void Login_WithTraversalUserId_ReturnsNull(string userId)
    {
        using var vault = new TempVault();
        Directory.CreateDirectory(vault.Root);

        Authentication.Login(vault.Root, userId, Password("pw")).Should().BeNull();
    }
}
