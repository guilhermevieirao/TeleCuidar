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

public class InvitesControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private string _adminToken = string.Empty;
    private Specialty _specialty = null!;

    public InvitesControllerTests(TestWebApplicationFactory<Program> factory)
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
        var uniqueCpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}";

        _specialty = new Specialty
        {
            Name = $"Test Specialty {Guid.NewGuid()}",
            Description = "Test Description"
        };
        context.Specialties.Add(_specialty);

        _adminUser = new User
        {
            Email = $"admin.invites.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Admin",
            LastName = "Invites",
            Cpf = uniqueCpf,
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

    #region GetInvites Tests

    [Fact]
    public async Task GetInvites_ShouldReturnOk_WhenAdmin()
    {
        // Act
        var response = await _client.GetAsync("/api/Invites?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
        content.Should().Contain("total");
    }

    [Fact]
    public async Task GetInvites_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/Invites?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInvites_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtService>();

        var rand = new Random();
        var patientUser = new User
        {
            Email = $"patient.invites.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Patient",
            LastName = "Test",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(patientUser);
        await context.SaveChangesAsync();

        var patientToken = jwtService.GenerateAccessToken(patientUser.Id, patientUser.Email, "PATIENT");
        var clientWithPatient = _factory.CreateClient();
        clientWithPatient.SetBearerToken(patientToken);

        // Act
        var response = await clientWithPatient.GetAsync("/api/Invites?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_ShouldReturnCreated_WithValidData()
    {
        // Arrange
        var request = new
        {
            Email = $"newinvite.{Guid.NewGuid()}@test.com",
            Role = "PROFESSIONAL",
            SpecialtyId = _specialty.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Invites", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("token");
    }

    [Fact]
    public async Task CreateInvite_ShouldReturnCreated_ForPatientRole()
    {
        // Arrange
        var request = new
        {
            Email = $"patient.invite.{Guid.NewGuid()}@test.com",
            Role = "PATIENT"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Invites", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region GenerateInviteLink Tests

    [Fact]
    public async Task GenerateInviteLink_ShouldReturnOk_WithValidData()
    {
        // Arrange
        var request = new
        {
            Email = $"linktest.{Guid.NewGuid()}@test.com",
            Role = "PROFESSIONAL",
            SpecialtyId = _specialty.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Invites/generate-link", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("link");
        content.Should().Contain("token");
        content.Should().Contain("expiresAt");
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public async Task ValidateToken_ShouldReturnOk_WithValidToken()
    {
        // Arrange - Create an invite first
        var createRequest = new
        {
            Email = $"validatetoken.{Guid.NewGuid()}@test.com",
            Role = "PATIENT"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/Invites", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var tokenStart = createContent.IndexOf("\"token\":\"") + 9;
        var tokenEnd = createContent.IndexOf("\"", tokenStart);
        var token = createContent.Substring(tokenStart, tokenEnd - tokenStart);

        // Use an unauthenticated client (ValidateToken is AllowAnonymous)
        var anonClient = _factory.CreateClient();

        // Act
        var response = await anonClient.GetAsync($"/api/Invites/validate/{token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("email");
        content.Should().Contain("role");
    }

    [Fact]
    public async Task ValidateToken_ShouldReturnNotFound_WithInvalidToken()
    {
        // Arrange
        var anonClient = _factory.CreateClient();
        var invalidToken = "invalid-token-12345";

        // Act
        var response = await anonClient.GetAsync($"/api/Invites/validate/{invalidToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetInvite Tests

    [Fact]
    public async Task GetInvite_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/Invites/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region CancelInvite Tests

    [Fact]
    public async Task CancelInvite_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsync($"/api/Invites/{invalidId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DeleteInvite Tests

    [Fact]
    public async Task DeleteInvite_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/Invites/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
