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

public class ScheduleBlocksControllerTests : IClassFixture<TestWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private User _adminUser = null!;
    private User _professionalUser = null!;
    private Specialty _specialty = null!;
    private ScheduleBlock _existingBlock = null!;
    private string _adminToken = null!;
    private string _professionalToken = null!;

    public ScheduleBlocksControllerTests(TestWebApplicationFactory<Program> factory)
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
            Description = "Especialidade para testes de bloqueios",
            Status = SpecialtyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Specialties.Add(_specialty);

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            LastName = "Block",
            Email = $"admin.block.{Guid.NewGuid()}@test.com",
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
            LastName = "Block",
            Email = $"prof.block.{Guid.NewGuid()}@test.com",
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
                Crm = "CRM55555",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Users.Add(_professionalUser);

        _existingBlock = new ScheduleBlock
        {
            Id = Guid.NewGuid(),
            ProfessionalId = _professionalUser.Id,
            Reason = "Férias",
            Type = ScheduleBlockType.Range,
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(20),
            Status = ScheduleBlockStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        context.ScheduleBlocks.Add(_existingBlock);

        await context.SaveChangesAsync();

        _adminToken = jwtService.GenerateAccessToken(_adminUser.Id, _adminUser.Email, "ADMIN");
        _professionalToken = jwtService.GenerateAccessToken(_professionalUser.Id, _professionalUser.Email, "PROFESSIONAL");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetScheduleBlocks_WithAuthentication_ReturnsPaginatedList()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/scheduleblocks?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
        content.Should().Contain("total");
        content.Should().Contain("page");
    }

    [Fact]
    public async Task GetScheduleBlocks_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/scheduleblocks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScheduleBlocks_WithProfessionalFilter_ReturnsFilteredList()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/scheduleblocks?professionalId={_professionalUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScheduleBlocks_WithStatusFilter_ReturnsFilteredList()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/scheduleblocks?status=Approved");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScheduleBlockById_ExistingBlock_ReturnsBlock()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/scheduleblocks/{_existingBlock.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Férias");
    }

    [Fact]
    public async Task GetScheduleBlockById_NonExistingBlock_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/scheduleblocks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateScheduleBlock_ValidData_ReturnsCreated()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _professionalToken);
        var dto = new
        {
            professionalId = _professionalUser.Id,
            type = "Single",
            date = DateTime.UtcNow.AddDays(30).ToString("o"),
            reason = "Consulta médica pessoal"
        };

        var response = await _client.PostAsJsonAsync("/api/scheduleblocks", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateScheduleBlock_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new
        {
            professionalId = _professionalUser.Id,
            type = "Single",
            date = DateTime.UtcNow.AddDays(30).ToString("o"),
            reason = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/scheduleblocks", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateScheduleBlock_ApprovedBlock_ReturnsConflict()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        var dto = new
        {
            reason = "Férias atualizadas"
        };

        // The existing block has status=Approved which cannot be updated
        var response = await _client.PatchAsJsonAsync($"/api/scheduleblocks/{_existingBlock.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteScheduleBlock_ExistingBlock_ReturnsNoContent()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        
        // Create a block to delete
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var blockToDelete = new ScheduleBlock
        {
            Id = Guid.NewGuid(),
            ProfessionalId = _professionalUser.Id,
            Reason = "Para deletar",
            Type = ScheduleBlockType.Single,
            Date = DateTime.UtcNow.AddDays(50),
            Status = ScheduleBlockStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.ScheduleBlocks.Add(blockToDelete);
        await context.SaveChangesAsync();

        var response = await _client.DeleteAsync($"/api/scheduleblocks/{blockToDelete.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteScheduleBlock_NonExistingBlock_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.DeleteAsync($"/api/scheduleblocks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
