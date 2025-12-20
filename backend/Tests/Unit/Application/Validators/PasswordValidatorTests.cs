using Application.Validators;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Application.Validators;

public class PasswordValidatorTests
{
    [Theory]
    [InlineData("Abcd1234@")]             // All requirements met
    [InlineData("StrongP@ss1")]           // All requirements met
    [InlineData("P@ssword123")]           // All requirements met
    [InlineData("Test!ng123")]            // All requirements met
    [InlineData("Complex$99")]            // All requirements met
    [InlineData("MyP@55word")]            // All requirements met
    [InlineData("Aa1!Aa1!")]              // Minimum length exactly
    public void IsValidPassword_ShouldReturnTrue_ForValidPasswords(string password)
    {
        // Act
        var result = CustomValidators.IsValidPassword(password);

        // Assert
        result.Should().BeTrue($"Password '{password}' should be valid");
    }

    [Theory]
    [InlineData(null)]                    // Null
    [InlineData("")]                      // Empty
    [InlineData("   ")]                   // Whitespace
    [InlineData("Ab1@")]                  // Too short
    [InlineData("Ab1@567")]               // Still too short (7 chars)
    [InlineData("abcd1234@")]             // Missing uppercase
    [InlineData("ABCD1234@")]             // Missing lowercase
    [InlineData("Abcdefgh@")]             // Missing number
    [InlineData("Abcd12345")]             // Missing special character
    [InlineData("12345678")]              // Only numbers
    [InlineData("abcdefgh")]              // Only lowercase
    [InlineData("ABCDEFGH")]              // Only uppercase
    [InlineData("@@@@@@@@")]              // Only special chars
    public void IsValidPassword_ShouldReturnFalse_ForInvalidPasswords(string? password)
    {
        // Act
        var result = CustomValidators.IsValidPassword(password!);

        // Assert
        result.Should().BeFalse($"Password '{password}' should be invalid");
    }

    [Fact]
    public void GetPasswordMissingRequirements_ShouldReturnAllMissing_ForEmptyPassword()
    {
        // Arrange
        var password = "";

        // Act
        var result = CustomValidators.GetPasswordMissingRequirements(password);

        // Assert
        result.Should().Contain("minimum 8 characters");
        result.Should().Contain("one uppercase letter");
        result.Should().Contain("one lowercase letter");
        result.Should().Contain("one number");
        result.Should().Contain("one special character (@$!%*?&)");
        result.Should().HaveCount(5);
    }

    [Fact]
    public void GetPasswordMissingRequirements_ShouldReturnEmpty_ForValidPassword()
    {
        // Arrange
        var password = "Abcd1234@";

        // Act
        var result = CustomValidators.GetPasswordMissingRequirements(password);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPasswordMissingRequirements_ShouldReturnSpecificMissing_ForPartialPassword()
    {
        // Arrange - Missing uppercase and special char
        var password = "abcd1234";

        // Act
        var result = CustomValidators.GetPasswordMissingRequirements(password);

        // Assert
        result.Should().Contain("one uppercase letter");
        result.Should().Contain("one special character (@$!%*?&)");
        result.Should().NotContain("minimum 8 characters");
        result.Should().NotContain("one lowercase letter");
        result.Should().NotContain("one number");
        result.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("@")]
    [InlineData("$")]
    [InlineData("!")]
    [InlineData("%")]
    [InlineData("*")]
    [InlineData("?")]
    [InlineData("&")]
    public void IsValidPassword_ShouldAcceptAllValidSpecialCharacters(string specialChar)
    {
        // Arrange
        var password = $"Abcd123{specialChar}";

        // Act
        var result = CustomValidators.IsValidPassword(password);

        // Assert
        result.Should().BeTrue($"Password with special char '{specialChar}' should be valid");
    }

    [Theory]
    [InlineData("#")]
    [InlineData("^")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("-")]
    [InlineData("_")]
    public void IsValidPassword_ShouldRejectOtherSpecialCharacters(string specialChar)
    {
        // Arrange - Only uses non-allowed special char
        var password = $"Abcd123{specialChar}";

        // Act
        var result = CustomValidators.IsValidPassword(password);

        // Assert
        result.Should().BeFalse($"Password with only special char '{specialChar}' should be invalid");
    }
}
