using System.Net;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Helpers;
using Xunit;

namespace Tests.Integration.WebAPI.Controllers;

public class AuditLogsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private string _adminToken = string.Empty;

    public AuditLogsControllerTests(TestWebApplicationFactory<Program> factory)
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
            Email = $"admin.audit.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Admin",
            LastName = "AuditLog",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.ADMIN,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(_adminUser);
        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
        _client.SetBearerToken(_adminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAuditLogs_ShouldReturnOk_WhenAuthenticatedAsAdmin()
    {
        // Act
        var response = await _client.GetAsync("/api/AuditLogs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnUnauthorized_WithoutAuth()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/AuditLogs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByAction()
    {
        // Act
        var response = await _client.GetAsync("/api/AuditLogs?page=1&pageSize=10&action=create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByEntityType()
    {
        // Act
        var response = await _client.GetAsync("/api/AuditLogs?page=1&pageSize=10&entityType=User");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByDateRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/AuditLogs?page=1&pageSize=10&startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnForbidden_WhenAuthenticatedAsPatient()
    {
        // Arrange - Cria paciente e autentica
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtService>();
        var rand = new Random();

        var patient = new User
        {
            Email = $"patient.audit.{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Name = "Paciente",
            LastName = "Teste",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active
        };
        context.Users.Add(patient);
        await context.SaveChangesAsync();

        var patientToken = jwtService.GenerateAccessToken(patient.Id, patient.Email, "PATIENT");
        _client.SetBearerToken(patientToken);

        // Act
        var response = await _client.GetAsync("/api/AuditLogs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
