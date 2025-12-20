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

public class AttachmentsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private User _professionalUser = null!;
    private User _patientUser = null!;
    private Specialty _specialty = null!;
    private Appointment _appointment = null!;
    private Attachment _existingAttachment = null!;
    private string _adminToken = null!;

    public AttachmentsControllerTests(TestWebApplicationFactory<Program> factory)
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
            Description = "Especialidade para testes de attachments",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            LastName = "Attach",
            Email = $"admin.attach.{Guid.NewGuid()}@test.com",
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
            LastName = "Attach",
            Email = $"prof.attach.{Guid.NewGuid()}@test.com",
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
                Crm = "CRM77777",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _patientUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Patient",
            LastName = "Attach",
            Email = $"patient.attach.{Guid.NewGuid()}@test.com",
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
            Time = TimeSpan.FromHours(14),
            Status = AppointmentStatus.Scheduled,
            Type = AppointmentType.Common,
            CreatedAt = DateTime.UtcNow
        };
        context.Appointments.Add(_appointment);

        _existingAttachment = new Attachment
        {
            Id = Guid.NewGuid(),
            AppointmentId = _appointment.Id,
            FileName = "test-file.pdf",
            FileType = "application/pdf",
            FileSize = 1024,
            FilePath = "/uploads/test-file.pdf",
            CreatedAt = DateTime.UtcNow
        };
        context.Attachments.Add(_existingAttachment);

        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAttachmentsByAppointment_WithAuthentication_ReturnsAttachments()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/attachments/appointment/{_appointment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("test-file.pdf");
    }

    [Fact]
    public async Task GetAttachmentsByAppointment_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/attachments/appointment/{_appointment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAttachment_ExistingAttachment_ReturnsAttachment()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/attachments/{_existingAttachment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("test-file.pdf");
    }

    [Fact]
    public async Task GetAttachment_NonExistingAttachment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/attachments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAttachment_ExistingAttachment_ReturnsSuccess()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        
        // Create a new attachment for deletion
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attachmentToDelete = new Attachment
        {
            Id = Guid.NewGuid(),
            AppointmentId = _appointment.Id,
            FileName = "delete-me.pdf",
            FileType = "application/pdf",
            FileSize = 512,
            FilePath = "/uploads/delete-me.pdf",
            CreatedAt = DateTime.UtcNow
        };
        context.Attachments.Add(attachmentToDelete);
        await context.SaveChangesAsync();

        var response = await _client.DeleteAsync($"/api/attachments/{attachmentToDelete.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAttachment_NonExistingAttachment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.DeleteAsync($"/api/attachments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadAttachment_NonExistingAttachment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/attachments/{Guid.NewGuid()}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
