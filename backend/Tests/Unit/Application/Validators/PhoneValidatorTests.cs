using Application.Validators;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Application.Validators;

public class PhoneValidatorTests
{
    [Theory]
    [InlineData("11999999999")]           // Celular válido
    [InlineData("(11) 99999-9999")]       // Celular com formatação
    [InlineData("1133333333")]            // Fixo válido
    [InlineData("(11) 3333-3333")]        // Fixo com formatação
    [InlineData("21987654321")]           // Outro celular válido
    [InlineData("11 98765-4321")]         // Celular com espaço
    public void IsValidPhone_ShouldReturnTrue_ForValidPhones(string phone)
    {
        // Act
        var result = CustomValidators.IsValidPhone(phone);

        // Assert
        result.Should().BeTrue($"Phone {phone} should be valid");
    }

    [Theory]
    [InlineData(null)]                    // Null
    [InlineData("")]                      // Empty
    [InlineData("   ")]                   // Whitespace
    [InlineData("123456789")]             // Too short (9 digits)
    [InlineData("123456789012")]          // Too long (12 digits)
    [InlineData("abcdefghijk")]           // Non-numeric
    public void IsValidPhone_ShouldReturnFalse_ForInvalidPhones(string? phone)
    {
        // Act
        var result = CustomValidators.IsValidPhone(phone!);

        // Assert
        result.Should().BeFalse($"Phone '{phone}' should be invalid");
    }

    [Theory]
    [InlineData("11999999999", "(11) 99999-9999")]
    [InlineData("1133333333", "(11) 3333-3333")]
    public void FormatPhone_ShouldFormatCorrectly(string input, string expected)
    {
        // Act
        var result = CustomValidators.FormatPhone(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatPhone_ShouldHandleMobilePhone()
    {
        // Arrange - Celular com 11 dígitos
        var phone = "21987654321";

        // Act
        var result = CustomValidators.FormatPhone(phone);

        // Assert
        result.Should().Be("(21) 98765-4321");
    }

    [Fact]
    public void FormatPhone_ShouldHandleLandlinePhone()
    {
        // Arrange - Fixo com 10 dígitos
        var phone = "1133334444";

        // Act
        var result = CustomValidators.FormatPhone(phone);

        // Assert
        result.Should().Be("(11) 3333-4444");
    }
}
