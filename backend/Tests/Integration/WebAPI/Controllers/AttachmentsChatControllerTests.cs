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

public class AttachmentsChatControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _professionalUser = null!;
    private User _patientUser = null!;
    private Specialty _specialty = null!;
    private Appointment _appointment = null!;

    public AttachmentsChatControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rand = new Random();

        _specialty = new Specialty
        {
            Id = Guid.NewGuid(),
            Name = $"Dermatologia {Guid.NewGuid()}",
            Description = "Especialidade para testes de attachments chat",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _professionalUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            LastName = "Chat",
            Email = $"prof.chat.{Guid.NewGuid()}@test.com",
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
                Crm = "CRM11111",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _patientUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Patient",
            LastName = "Chat",
            Email = $"patient.chat.{Guid.NewGuid()}@test.com",
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
            Time = TimeSpan.FromHours(11),
            Status = AppointmentStatus.InProgress,
            Type = AppointmentType.Common,
            CreatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(_appointment);

        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMessages_ExistingAppointment_ReturnsEmptyList()
    {
        // AttachmentsChatController doesn't have [Authorize]
        var response = await _client.GetAsync($"/api/appointments/{_appointment.Id}/attachments-chat");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_NonExistingAppointment_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/appointments/{Guid.NewGuid()}/attachments-chat");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMessage_ExistingAppointment_ReturnsSuccess()
    {
        var dto = new
        {
            id = Guid.NewGuid().ToString(),
            senderId = _patientUser.Id.ToString(),
            senderName = "Patient Chat",
            senderRole = "PATIENT",
            fileUrl = "/uploads/test-image.jpg",
            fileName = "test-image.jpg",
            fileType = "image/jpeg",
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var response = await _client.PostAsJsonAsync($"/api/appointments/{_appointment.Id}/attachments-chat", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddMessage_NonExistingAppointment_ReturnsNotFound()
    {
        var dto = new
        {
            id = Guid.NewGuid().ToString(),
            senderId = _patientUser.Id.ToString(),
            senderName = "Patient",
            fileUrl = "/uploads/test.pdf",
            fileName = "test.pdf",
            fileType = "application/pdf"
        };

        var response = await _client.PostAsJsonAsync($"/api/appointments/{Guid.NewGuid()}/attachments-chat", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMessage_NonExistingAppointment_ReturnsNotFound()
    {
        var messageId = Guid.NewGuid().ToString();

        var response = await _client.DeleteAsync($"/api/appointments/{Guid.NewGuid()}/attachments-chat/{messageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
