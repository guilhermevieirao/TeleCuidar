using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class AuditLogTests
{
    [Fact]
    public void AuditLog_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.Id.Should().NotBeEmpty();
        auditLog.Action.Should().BeEmpty();
        auditLog.EntityType.Should().BeEmpty();
        auditLog.EntityId.Should().BeEmpty();
        auditLog.UserId.Should().BeNull();
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().BeNull();
        auditLog.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
        auditLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AuditLog_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var auditLog = new AuditLog();
        var userId = Guid.NewGuid();
        var action = "create";
        var entityType = "User";
        var entityId = Guid.NewGuid().ToString();
        var oldValues = "{\"name\":\"Old Name\"}";
        var newValues = "{\"name\":\"New Name\"}";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        auditLog.UserId = userId;
        auditLog.Action = action;
        auditLog.EntityType = entityType;
        auditLog.EntityId = entityId;
        auditLog.OldValues = oldValues;
        auditLog.NewValues = newValues;
        auditLog.IpAddress = ipAddress;
        auditLog.UserAgent = userAgent;

        // Assert
        auditLog.UserId.Should().Be(userId);
        auditLog.Action.Should().Be(action);
        auditLog.EntityType.Should().Be(entityType);
        auditLog.EntityId.Should().Be(entityId);
        auditLog.OldValues.Should().Be(oldValues);
        auditLog.NewValues.Should().Be(newValues);
        auditLog.IpAddress.Should().Be(ipAddress);
        auditLog.UserAgent.Should().Be(userAgent);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("login")]
    [InlineData("logout")]
    public void AuditLog_ShouldAcceptDifferentActionTypes(string action)
    {
        // Arrange
        var auditLog = new AuditLog();

        // Act
        auditLog.Action = action;

        // Assert
        auditLog.Action.Should().Be(action);
    }

    [Fact]
    public void AuditLog_ShouldAllowNullUserId()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Action = "system_event",
            EntityType = "System",
            EntityId = "0"
        };

        // Assert
        auditLog.UserId.Should().BeNull();
    }

    [Fact]
    public void AuditLog_ShouldLinkToUser()
    {
        // Arrange
        var user = new User { Name = "Admin", Role = UserRole.ADMIN };
        var auditLog = new AuditLog
        {
            UserId = user.Id,
            User = user,
            Action = "login",
            EntityType = "User",
            EntityId = user.Id.ToString()
        };

        // Assert
        auditLog.UserId.Should().Be(user.Id);
        auditLog.User.Should().Be(user);
    }
}
