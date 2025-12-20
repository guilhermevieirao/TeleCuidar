using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class InviteTests
{
    [Fact]
    public void Invite_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var invite = new Invite();

        // Assert
        invite.Id.Should().BeEmpty();
        invite.Token.Should().BeEmpty();
        invite.Status.Should().Be(InviteStatus.Pending);
        invite.Email.Should().BeNull();
        invite.SpecialtyId.Should().BeNull();
        invite.AcceptedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(InviteStatus.Pending)]
    [InlineData(InviteStatus.Accepted)]
    [InlineData(InviteStatus.Expired)]
    [InlineData(InviteStatus.Cancelled)]
    public void Invite_ShouldAcceptAllValidStatuses(InviteStatus status)
    {
        // Arrange
        var invite = new Invite();

        // Act
        invite.Status = status;

        // Assert
        invite.Status.Should().Be(status);
    }

    [Fact]
    public void Invite_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var invite = new Invite();
        var id = Guid.NewGuid();
        var email = "convidado@example.com";
        var token = "abc123token";
        var role = UserRole.PROFESSIONAL;
        var specialtyId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var createdBy = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        invite.Id = id;
        invite.Email = email;
        invite.Token = token;
        invite.Role = role;
        invite.SpecialtyId = specialtyId;
        invite.ExpiresAt = expiresAt;
        invite.CreatedBy = createdBy;
        invite.CreatedAt = createdAt;

        // Assert
        invite.Id.Should().Be(id);
        invite.Email.Should().Be(email);
        invite.Token.Should().Be(token);
        invite.Role.Should().Be(role);
        invite.SpecialtyId.Should().Be(specialtyId);
        invite.ExpiresAt.Should().Be(expiresAt);
        invite.CreatedBy.Should().Be(createdBy);
        invite.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Invite_ShouldAllowNullEmailForGenericLinks()
    {
        // Arrange
        var invite = new Invite
        {
            Token = "generic-token",
            Role = UserRole.PATIENT
        };

        // Assert
        invite.Email.Should().BeNull();
    }

    [Fact]
    public void Invite_ShouldSetAcceptedAt()
    {
        // Arrange
        var invite = new Invite
        {
            Status = InviteStatus.Pending
        };
        var acceptedAt = DateTime.UtcNow;

        // Act
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = acceptedAt;

        // Assert
        invite.Status.Should().Be(InviteStatus.Accepted);
        invite.AcceptedAt.Should().Be(acceptedAt);
    }
}
