using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class ScheduleTests
{
    [Fact]
    public void Schedule_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var schedule = new Schedule();

        // Assert
        schedule.Id.Should().NotBeEmpty();
        schedule.ProfessionalId.Should().BeEmpty();
        schedule.GlobalConfigJson.Should().BeEmpty();
        schedule.DaysConfigJson.Should().BeEmpty();
        schedule.IsActive.Should().BeTrue();
        schedule.ValidityEndDate.Should().BeNull();
        schedule.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Schedule_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var schedule = new Schedule();
        var professionalId = Guid.NewGuid();
        var globalConfig = "{\"slotDuration\":30,\"breakTime\":10}";
        var daysConfig = "[{\"dayOfWeek\":1,\"startTime\":\"08:00\",\"endTime\":\"18:00\"}]";
        var validityStart = new DateTime(2025, 1, 1);
        var validityEnd = new DateTime(2025, 12, 31);

        // Act
        schedule.ProfessionalId = professionalId;
        schedule.GlobalConfigJson = globalConfig;
        schedule.DaysConfigJson = daysConfig;
        schedule.ValidityStartDate = validityStart;
        schedule.ValidityEndDate = validityEnd;
        schedule.IsActive = false;

        // Assert
        schedule.ProfessionalId.Should().Be(professionalId);
        schedule.GlobalConfigJson.Should().Be(globalConfig);
        schedule.DaysConfigJson.Should().Be(daysConfig);
        schedule.ValidityStartDate.Should().Be(validityStart);
        schedule.ValidityEndDate.Should().Be(validityEnd);
        schedule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Schedule_ShouldLinkToProfessional()
    {
        // Arrange
        var professional = new User { Name = "Dr. João", Role = UserRole.PROFESSIONAL };
        var schedule = new Schedule
        {
            ProfessionalId = professional.Id,
            Professional = professional
        };

        // Assert
        schedule.ProfessionalId.Should().Be(professional.Id);
        schedule.Professional.Should().Be(professional);
    }
}
