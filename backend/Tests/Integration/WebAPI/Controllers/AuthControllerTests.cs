using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Helpers;
using Xunit;

namespace Tests.Integration.WebAPI.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WithValidData()
    {
        // Arrange
        var request = new
        {
            Name = "João",
            LastName = "Silva",
            Email = $"joao.silva.{Guid.NewGuid()}@test.com",
            Cpf = "12345678909",
            Phone = "11999999999",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            AcceptTerms = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("user");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WithMismatchedPasswords()
    {
        // Arrange
        var request = new
        {
            Name = "João",
            LastName = "Silva",
            Email = $"test.{Guid.NewGuid()}@test.com",
            Cpf = "52998224725",
            Password = "Test@123",
            ConfirmPassword = "Different@123",
            AcceptTerms = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WithoutAcceptingTerms()
    {
        // Arrange
        var request = new
        {
            Name = "João",
            LastName = "Silva",
            Email = $"test.{Guid.NewGuid()}@test.com",
            Cpf = "11144477735",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            AcceptTerms = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("accept terms");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WithWeakPassword()
    {
        // Arrange
        var request = new
        {
            Name = "João",
            LastName = "Silva",
            Email = $"test.{Guid.NewGuid()}@test.com",
            Cpf = "98765432109",
            Password = "weak",
            ConfirmPassword = "weak",
            AcceptTerms = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Password");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithInvalidCredentials()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword@123",
            RememberMe = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WithInvalidToken()
    {
        // Arrange
        var request = new
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnBadRequest_WithInvalidToken()
    {
        // Arrange
        var request = new
        {
            Token = "invalid-verification-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/verify-email", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
