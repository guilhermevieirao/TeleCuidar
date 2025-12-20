using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Email.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.Name.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.Cpf.Should().BeEmpty();
        user.Status.Should().Be(UserStatus.Active);
        user.EmailVerified.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(UserRole.PATIENT)]
    [InlineData(UserRole.PROFESSIONAL)]
    [InlineData(UserRole.ADMIN)]
    public void User_ShouldAcceptAllValidRoles(UserRole role)
    {
        // Arrange
        var user = new User();

        // Act
        user.Role = role;

        // Assert
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Inactive)]
    public void User_ShouldAcceptAllValidStatuses(UserStatus status)
    {
        // Arrange
        var user = new User();

        // Act
        user.Status = status;

        // Assert
        user.Status.Should().Be(status);
    }

    [Fact]
    public void User_ShouldInitializeCollections()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.AppointmentsAsPatient.Should().NotBeNull().And.BeEmpty();
        user.AppointmentsAsProfessional.Should().NotBeNull().And.BeEmpty();
        user.Notifications.Should().NotBeNull().And.BeEmpty();
        user.Schedules.Should().NotBeNull().And.BeEmpty();
        user.AuditLogs.Should().NotBeNull().And.BeEmpty();
        user.ScheduleBlocks.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var user = new User();
        var email = "test@example.com";
        var name = "João";
        var lastName = "Silva";
        var cpf = "12345678901";
        var phone = "11999999999";
        var avatar = "https://example.com/avatar.jpg";

        // Act
        user.Email = email;
        user.Name = name;
        user.LastName = lastName;
        user.Cpf = cpf;
        user.Phone = phone;
        user.Avatar = avatar;
        user.Role = UserRole.PATIENT;
        user.EmailVerified = true;

        // Assert
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.LastName.Should().Be(lastName);
        user.Cpf.Should().Be(cpf);
        user.Phone.Should().Be(phone);
        user.Avatar.Should().Be(avatar);
        user.Role.Should().Be(UserRole.PATIENT);
        user.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public void User_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var user1 = new User();
        var user2 = new User();

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void User_PasswordResetToken_ShouldBeNullableWithExpiry()
    {
        // Arrange
        var user = new User();
        var token = Guid.NewGuid().ToString();
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = expiry;

        // Assert
        user.PasswordResetToken.Should().Be(token);
        user.PasswordResetTokenExpiry.Should().Be(expiry);
    }

    [Fact]
    public void User_RefreshToken_ShouldBeNullableWithExpiry()
    {
        // Arrange
        var user = new User();
        var token = "refresh-token-value";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.RefreshToken = token;
        user.RefreshTokenExpiry = expiry;

        // Assert
        user.RefreshToken.Should().Be(token);
        user.RefreshTokenExpiry.Should().Be(expiry);
    }
}
