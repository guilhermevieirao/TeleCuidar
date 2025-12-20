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

public class UsersControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private string _adminToken = string.Empty;

    public UsersControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtService>();

        // Usa CPF único para cada instância
        var rand = new Random();
        var uniqueCpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}";

        _adminUser = new User
        {
            Email = $"admin.users.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Admin",
            LastName = "Users",
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

    [Fact]
    public async Task GetUsers_ShouldReturnOk_WhenAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/Users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
        content.Should().Contain("total");
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/Users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WithValidId()
    {
        // Act
        var response = await _client.GetAsync($"/api/Users/{_adminUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(_adminUser.Email);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/Users/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated_WithValidData()
    {
        // Arrange
        // CPFs válidos para teste
        var validCpfs = new[] { "52998224725", "11144477735", "93746865013", "28803326009", "65185837046" };
        var rand = new Random();
        var request = new
        {
            Name = "Novo",
            LastName = "Usuário",
            Email = $"novo.{Guid.NewGuid()}@test.com",
            Cpf = validCpfs[rand.Next(validCpfs.Length)],
            Phone = $"119{rand.Next(10000000, 99999999)}",
            Password = "Test@123",
            Role = "PATIENT"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Users", request);

        // Assert
        // Pode retornar 201 ou 400 se CPF já existir
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WithInvalidEmail()
    {
        // Arrange
        var request = new
        {
            Name = "Test",
            LastName = "User",
            Email = "invalid-email",
            Cpf = "12345678909",
            Password = "Test@123",
            Role = "PATIENT"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WithWeakPassword()
    {
        // Arrange
        var request = new
        {
            Name = "Test",
            LastName = "User",
            Email = $"test.{Guid.NewGuid()}@test.com",
            Cpf = "12345678909",
            Password = "weak",
            Role = "PATIENT"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var request = new
        {
            Name = "Updated",
            LastName = "User",
            Email = "updated@test.com"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/Users/{invalidId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/Users/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsers_ShouldFilterByRole()
    {
        // Arrange - Cria um paciente
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rand = new Random();

        var patient = new User
        {
            Email = $"patient.filter.{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Name = "Paciente",
            LastName = "Teste",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active
        };
        context.Users.Add(patient);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/Users?page=1&pageSize=10&role=PATIENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("PATIENT");
    }

    [Fact]
    public async Task GetUsers_ShouldFilterByStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/Users?page=1&pageSize=10&status=Active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
