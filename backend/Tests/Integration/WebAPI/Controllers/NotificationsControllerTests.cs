using System.Net;
using System.Net.Http.Json;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Helpers;
using Xunit;

namespace Tests.Integration.WebAPI.Controllers;

public class NotificationsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private User _patientUser = null!;
    private string _adminToken = string.Empty;
    private Notification _notification = null!;

    public NotificationsControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtService>();

        var rand = new Random();

        _adminUser = new User
        {
            Email = $"admin.notifications.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Admin",
            LastName = "Notifications",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.ADMIN,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(_adminUser);

        _patientUser = new User
        {
            Email = $"patient.notifications.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Patient",
            LastName = "Notifications",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(_patientUser);
        await context.SaveChangesAsync();

        _notification = new Notification
        {
            UserId = _patientUser.Id,
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = "info",
            IsRead = false
        };
        context.Notifications.Add(_notification);
        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
        _client.SetBearerToken(_adminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotifications_ShouldReturnOk_WhenAuthenticated()
    {
        // Act
        var response = await _client.GetAsync($"/api/Notifications/user/{_patientUser.Id}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
        content.Should().Contain("total");
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync($"/api/Notifications/user/{_patientUser.Id}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_ShouldFilterByIsRead()
    {
        // Act
        var response = await _client.GetAsync($"/api/Notifications/user/{_patientUser.Id}?isRead=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GetUnreadCount Tests

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOk_WhenAuthenticated()
    {
        // Act
        var response = await _client.GetAsync($"/api/Notifications/user/{_patientUser.Id}/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("count");
    }

    #endregion

    #region CreateNotification Tests

    [Fact]
    public async Task CreateNotification_ShouldReturnCreated_WithValidData()
    {
        // Arrange
        var request = new
        {
            UserId = _patientUser.Id,
            Title = "New Notification",
            Message = "You have a new appointment",
            Type = "appointment"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("New Notification");
    }

    [Fact]
    public async Task CreateNotification_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtService>();
        var patientToken = jwtService.GenerateAccessToken(_patientUser.Id, _patientUser.Email, "PATIENT");
        
        var clientWithPatient = _factory.CreateClient();
        clientWithPatient.SetBearerToken(patientToken);

        var request = new
        {
            UserId = _patientUser.Id,
            Title = "New Notification",
            Message = "Test message",
            Type = "info"
        };

        // Act
        var response = await clientWithPatient.PostAsJsonAsync("/api/Notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WithValidId()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Notifications/{_notification.Id}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("marked as read");
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsync($"/api/Notifications/{invalidId}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region MarkAllAsRead Tests

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnOk()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Notifications/user/{_patientUser.Id}/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("All notifications marked as read");
    }

    #endregion
}
