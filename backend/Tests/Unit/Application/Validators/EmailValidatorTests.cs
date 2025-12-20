using Application.Validators;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Application.Validators;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.com")]
    [InlineData("user+tag@example.org")]
    [InlineData("user@subdomain.domain.com")]
    [InlineData("a@b.co")]
    [InlineData("email123@gmail.com")]
    public void IsValidEmail_ShouldReturnTrue_ForValidEmails(string email)
    {
        // Act
        var result = CustomValidators.IsValidEmail(email);

        // Assert
        result.Should().BeTrue($"Email '{email}' should be valid");
    }

    [Theory]
    [InlineData(null)]                      // Null
    [InlineData("")]                        // Empty
    [InlineData("   ")]                     // Whitespace
    [InlineData("notanemail")]              // No @ sign
    [InlineData("@nodomain.com")]           // Missing local part
    [InlineData("test@")]                   // Missing domain
    [InlineData("test@.com")]               // Invalid domain
    [InlineData("test @example.com")]       // Space in email
    public void IsValidEmail_ShouldReturnFalse_ForInvalidEmails(string? email)
    {
        // Act
        var result = CustomValidators.IsValidEmail(email!);

        // Assert
        result.Should().BeFalse($"Email '{email}' should be invalid");
    }

    [Fact]
    public void IsValidEmail_ShouldBeCaseSensitive()
    {
        // Arrange
        var email1 = "Test@Example.com";
        var email2 = "TEST@EXAMPLE.COM";

        // Act
        var result1 = CustomValidators.IsValidEmail(email1);
        var result2 = CustomValidators.IsValidEmail(email2);

        // Assert - Both should be valid as email addresses are case-insensitive
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
