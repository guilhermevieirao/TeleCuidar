using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class SpecialtyTests
{
    [Fact]
    public void Specialty_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var specialty = new Specialty();

        // Assert
        specialty.Id.Should().NotBeEmpty();
        specialty.Name.Should().BeEmpty();
        specialty.Description.Should().BeEmpty();
        specialty.Status.Should().Be(SpecialtyStatus.Active);
        specialty.Professionals.Should().NotBeNull().And.BeEmpty();
        specialty.Appointments.Should().NotBeNull().And.BeEmpty();
        specialty.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(SpecialtyStatus.Active)]
    [InlineData(SpecialtyStatus.Inactive)]
    public void Specialty_ShouldAcceptAllValidStatuses(SpecialtyStatus status)
    {
        // Arrange
        var specialty = new Specialty();

        // Act
        specialty.Status = status;

        // Assert
        specialty.Status.Should().Be(status);
    }

    [Fact]
    public void Specialty_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var specialty = new Specialty();
        var name = "Cardiologia";
        var description = "Especialidade médica para tratamento do coração";
        var customFields = "[{\"name\":\"pressao\",\"type\":\"text\"}]";

        // Act
        specialty.Name = name;
        specialty.Description = description;
        specialty.CustomFieldsJson = customFields;

        // Assert
        specialty.Name.Should().Be(name);
        specialty.Description.Should().Be(description);
        specialty.CustomFieldsJson.Should().Be(customFields);
    }

    [Fact]
    public void Specialty_ShouldAllowNullCustomFields()
    {
        // Arrange
        var specialty = new Specialty();

        // Assert
        specialty.CustomFieldsJson.Should().BeNull();
    }
}
