using Application.Validators;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Application.Validators;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("12345678909")]      // CPF válido sem formatação
    [InlineData("123.456.789-09")]   // CPF válido com formatação
    [InlineData("52998224725")]      // Outro CPF válido
    [InlineData("529.982.247-25")]   // Outro CPF válido com formatação
    public void IsValidCpf_ShouldReturnTrue_ForValidCpfs(string cpf)
    {
        // Act
        var result = CustomValidators.IsValidCpf(cpf);

        // Assert
        result.Should().BeTrue($"CPF {cpf} should be valid");
    }

    [Theory]
    [InlineData(null)]                // Null
    [InlineData("")]                  // Empty
    [InlineData("   ")]               // Whitespace
    [InlineData("12345678901")]       // Invalid verification digits
    [InlineData("00000000000")]       // All same digits
    [InlineData("11111111111")]       // All same digits
    [InlineData("22222222222")]       // All same digits
    [InlineData("33333333333")]       // All same digits
    [InlineData("44444444444")]       // All same digits
    [InlineData("55555555555")]       // All same digits
    [InlineData("66666666666")]       // All same digits
    [InlineData("77777777777")]       // All same digits
    [InlineData("88888888888")]       // All same digits
    [InlineData("99999999999")]       // All same digits
    [InlineData("1234567890")]        // Too short
    [InlineData("123456789012")]      // Too long
    [InlineData("abc.def.ghi-jk")]    // Non-numeric
    public void IsValidCpf_ShouldReturnFalse_ForInvalidCpfs(string? cpf)
    {
        // Act
        var result = CustomValidators.IsValidCpf(cpf!);

        // Assert
        result.Should().BeFalse($"CPF '{cpf}' should be invalid");
    }

    [Theory]
    [InlineData("12345678909", "123.456.789-09")]
    [InlineData("123.456.789-09", "123.456.789-09")]
    [InlineData("52998224725", "529.982.247-25")]
    public void FormatCpf_ShouldFormatCorrectly(string input, string expected)
    {
        // Act
        var result = CustomValidators.FormatCpf(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1234")]              // Too short
    [InlineData("12345678901234")]    // Too long
    public void FormatCpf_ShouldReturnOriginal_ForInvalidLength(string input)
    {
        // Act
        var result = CustomValidators.FormatCpf(input);

        // Assert - Deve retornar apenas os dígitos
        result.Should().NotContain(".");
        result.Should().NotContain("-");
    }
}
