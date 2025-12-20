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

public class SchedulesControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private User _professional = null!;
    private string _adminToken = string.Empty;

    public SchedulesControllerTests(TestWebApplicationFactory<Program> factory)
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
            Email = $"admin.sched.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Admin",
            LastName = "Schedules",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.ADMIN,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(_adminUser);

        _professional = new User
        {
            Email = $"prof.sched.{Guid.NewGuid()}@test.com",
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y",
            Name = "Dr. Profissional",
            LastName = "Teste",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            Role = UserRole.PROFESSIONAL,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        context.Users.Add(_professional);

        await context.SaveChangesAsync();

        // Cria perfil profissional
        var professionalProfile = new ProfessionalProfile
        {
            UserId = _professional.Id
        };
        context.ProfessionalProfiles.Add(professionalProfile);
        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
        _client.SetBearerToken(_adminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetSchedules_ShouldReturnOk_WhenAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/Schedules?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSchedules_ShouldReturnUnauthorized_WithoutAuth()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/Schedules?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSchedule_ShouldReturnCreated_WithValidData()
    {
        // Arrange
        var request = new
        {
            ProfessionalId = _professional.Id,
            ProfessionalName = $"{_professional.Name} {_professional.LastName}",
            ProfessionalEmail = _professional.Email,
            GlobalConfig = new
            {
                TimeRange = new { StartTime = "08:00", EndTime = "18:00" },
                BreakTime = new { StartTime = "12:00", EndTime = "13:00" },
                ConsultationDuration = 30,
                IntervalBetweenConsultations = 10
            },
            DaysConfig = new[]
            {
                new { Day = "Monday", IsWorking = true, Customized = false },
                new { Day = "Tuesday", IsWorking = true, Customized = false },
                new { Day = "Wednesday", IsWorking = true, Customized = false },
                new { Day = "Thursday", IsWorking = true, Customized = false },
                new { Day = "Friday", IsWorking = true, Customized = false }
            },
            ValidityStartDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            Status = "Active"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Schedules", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetScheduleById_ShouldReturnNotFound_WithInvalidId()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/Schedules/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScheduleByProfessional_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync($"/api/Schedules/professional/{_professional.Id}");

        // Assert
        // Pode retornar 200 (agenda encontrada) ou 404 (sem agenda)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
