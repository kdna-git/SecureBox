using FluentAssertions;
using SecureBox.Methods;
using Xunit;

namespace SecureBox.Test;

public class IsValidUserIdTests
{
    [Theory]
    [InlineData("alice")]
    [InlineData("user name")]
    [InlineData("user.name")]
    [InlineData("user-name")]
    [InlineData("user_name")]
    [InlineData("usuário")]
    [InlineData("...")]
    public void AcceptsNamesThatCannotEscapeTheVault(string userId)
    {
        Authentication.IsValidUserId(userId).Should().BeTrue();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("a/b/c")]
    public void RejectsTraversalAndSeparators(string userId)
    {
        Authentication.IsValidUserId(userId).Should().BeFalse();
    }

    [Fact]
    public void RejectsCharactersThatAreInvalidFileNamesOnWindows()
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            Authentication.IsValidUserId($"a{invalid}b").Should().BeFalse();
    }
}
