using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Helpers;
using Xunit;

namespace Tests.Integration.WebAPI.Controllers;

public class ReportsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private User _professionalUser = null!;
    private User _patientUser = null!;
    private Specialty _specialty = null!;
    private string _adminToken = null!;
    private string _professionalToken = null!;
    private string _patientToken = null!;

    public ReportsControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

        var rand = new Random();

        _specialty = new Specialty
        {
            Id = Guid.NewGuid(),
            Name = $"Clínica Geral {Guid.NewGuid()}",
            Description = "Especialidade para testes de relatórios",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            LastName = "Report",
            Email = $"admin.report.{Guid.NewGuid()}@test.com",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            PasswordHash = "hashed",
            Role = UserRole.ADMIN,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(_adminUser);

        _professionalUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            LastName = "Report",
            Email = $"prof.report.{Guid.NewGuid()}@test.com",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            PasswordHash = "hashed",
            Role = UserRole.PROFESSIONAL,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            ProfessionalProfile = new ProfessionalProfile
            {
                Id = Guid.NewGuid(),
                SpecialtyId = _specialty.Id,
                Crm = "CRM12345",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _patientUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Patient",
            LastName = "Report",
            Email = $"patient.report.{Guid.NewGuid()}@test.com",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            PasswordHash = "hashed",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(_patientUser);

        // Create some appointments for reports
        var appointments = new List<Appointment>
        {
            new() {
                Id = Guid.NewGuid(),
                PatientId = _patientUser.Id,
                ProfessionalId = _professionalUser.Id,
                SpecialtyId = _specialty.Id,
                Date = DateTime.UtcNow.AddDays(-1),
                Time = TimeSpan.FromHours(10),
                Status = AppointmentStatus.Completed,
                Type = AppointmentType.Common,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                PatientId = _patientUser.Id,
                ProfessionalId = _professionalUser.Id,
                SpecialtyId = _specialty.Id,
                Date = DateTime.UtcNow.AddDays(1),
                Time = TimeSpan.FromHours(14),
                Status = AppointmentStatus.Scheduled,
                Type = AppointmentType.Common,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Appointments.AddRange(appointments);

        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
        _professionalToken = jwtService.GenerateAccessToken(_professionalUser.Id, _professionalUser.Email, "PROFESSIONAL");
        _patientToken = jwtService.GenerateAccessToken(_patientUser.Id, _patientUser.Email, "PATIENT");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboardStats_WithAuthentication_ReturnsStats()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("users");
        content.Should().Contain("appointments");
    }

    [Fact]
    public async Task GetDashboardStats_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardStats_WithUserIdFilter_ReturnsFilteredStats()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);

        var response = await _client.GetAsync($"/api/reports/dashboard?userId={_professionalUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateAppointmentsReport_AsAdmin_ReturnsReport()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/reports/appointments?startDate={startDate}&endDate={endDate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateAppointmentsReport_AsNonAdmin_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _patientToken);
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/reports/appointments?startDate={startDate}&endDate={endDate}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSpecialtyStats_AsAdmin_ReturnsStats()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/reports/specialties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSpecialtyStats_AsNonAdmin_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);

        var response = await _client.GetAsync("/api/reports/specialties");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReportData_AsAdmin_ReturnsConsolidatedData()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/reports?startDate={startDate}&endDate={endDate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("statistics");
        content.Should().Contain("usersByRole");
        content.Should().Contain("appointmentsByStatus");
    }

    [Fact]
    public async Task GetReportData_AsNonAdmin_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _patientToken);
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/reports?startDate={startDate}&endDate={endDate}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
