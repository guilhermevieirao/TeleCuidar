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

public class BiometricsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _professionalUser = null!;
    private User _patientUser = null!;
    private Specialty _specialty = null!;
    private Appointment _appointment = null!;
    private string _patientToken = null!;

    public BiometricsControllerTests(TestWebApplicationFactory<Program> factory)
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
            Name = $"Cardiologia {Guid.NewGuid()}",
            Description = "Especialidade para testes de biométricos",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _professionalUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            LastName = "Bio",
            Email = $"prof.bio.{Guid.NewGuid()}@test.com",
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
                Crm = "CRM99999",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _patientUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Patient",
            LastName = "Bio",
            Email = $"patient.bio.{Guid.NewGuid()}@test.com",
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
            Time = TimeSpan.FromHours(9),
            Status = AppointmentStatus.InProgress,
            Type = AppointmentType.Common,
            CreatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(_appointment);

        await context.SaveChangesAsync();

        _patientToken = jwtService.GenerateAccessToken(_patientUser.Id, _patientUser.Email, "PATIENT");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBiometrics_ExistingAppointment_ReturnsData()
    {
        // BiometricsController doesn't have [Authorize] so it's open
        var response = await _client.GetAsync($"/api/appointments/{_appointment.Id}/biometrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBiometrics_NonExistingAppointment_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/appointments/{Guid.NewGuid()}/biometrics");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBiometrics_ExistingAppointment_ReturnsSuccess()
    {
        var dto = new
        {
            heartRate = 72,
            bloodPressure = "120/80",
            temperature = 36.5,
            oxygenSaturation = 98
        };

        var response = await _client.PutAsJsonAsync($"/api/appointments/{_appointment.Id}/biometrics", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateBiometrics_NonExistingAppointment_ReturnsNotFound()
    {
        var dto = new
        {
            heartRate = 72,
            bloodPressure = "120/80"
        };

        var response = await _client.PutAsJsonAsync($"/api/appointments/{Guid.NewGuid()}/biometrics", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckUpdate_NonExistingAppointment_ReturnsNotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, $"/api/appointments/{Guid.NewGuid()}/biometrics");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
