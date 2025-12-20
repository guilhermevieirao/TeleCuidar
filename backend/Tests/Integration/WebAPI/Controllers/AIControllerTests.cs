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

public class AIControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _professionalUser = null!;
    private User _patientUser = null!;
    private Specialty _specialty = null!;
    private Appointment _appointment = null!;
    private string _professionalToken = null!;

    public AIControllerTests(TestWebApplicationFactory<Program> factory)
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
            Name = $"Neurologia {Guid.NewGuid()}",
            Description = "Especialidade para testes de AI",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _professionalUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            LastName = "AI",
            Email = $"prof.ai.{Guid.NewGuid()}@test.com",
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
                Crm = "CRM88888",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _patientUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Patient",
            LastName = "AI",
            Email = $"patient.ai.{Guid.NewGuid()}@test.com",
            Cpf = $"{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(100, 999)}{rand.Next(10, 99)}",
            PasswordHash = "hashed",
            Role = UserRole.PATIENT,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(_patientUser);

        _appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = _patientUser.Id,
            ProfessionalId = _professionalUser.Id,
            SpecialtyId = _specialty.Id,
            Date = DateTime.UtcNow.AddDays(1),
            Time = TimeSpan.FromHours(10),
            Status = AppointmentStatus.InProgress,
            Type = AppointmentType.Common,
            CreatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(_appointment);

        await context.SaveChangesAsync();

        _professionalToken = jwtService.GenerateAccessToken(_professionalUser.Id, _professionalUser.Email, "PROFESSIONAL");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAIData_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/ai/appointment/{_appointment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAIData_ExistingAppointment_ReturnsData()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);

        var response = await _client.GetAsync($"/api/ai/appointment/{_appointment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAIData_NonExistingAppointment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);

        var response = await _client.GetAsync($"/api/ai/appointment/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveAIData_ExistingAppointment_ReturnsSuccess()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);
        var dto = new
        {
            summary = "Paciente apresentou sintomas de...",
            diagnosticHypothesis = "Possível diagnóstico de..."
        };

        var response = await _client.PutAsJsonAsync($"/api/ai/appointment/{_appointment.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveAIData_NonExistingAppointment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);
        var dto = new
        {
            summary = "Test summary",
            diagnosticHypothesis = "Test hypothesis"
        };

        var response = await _client.PutAsJsonAsync($"/api/ai/appointment/{Guid.NewGuid()}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateSummary_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new
        {
            appointmentId = _appointment.Id,
            patientName = "Test Patient"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/summary", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateDiagnosis_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new
        {
            appointmentId = _appointment.Id,
            patientName = "Test Patient"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/diagnosis", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
