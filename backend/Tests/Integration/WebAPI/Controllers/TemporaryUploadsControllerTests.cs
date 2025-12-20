using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Helpers;
using Xunit;

namespace Tests.Integration.WebAPI.Controllers;

public class TemporaryUploadsControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TemporaryUploadsControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task StoreUpload_ValidData_ReturnsSuccess()
    {
        var token = Guid.NewGuid().ToString();
        var dto = new
        {
            fileUrl = "/uploads/temp-file.pdf",
            fileName = "temp-file.pdf",
            fileType = "application/pdf",
            fileSize = 1024
        };

        var response = await _client.PostAsJsonAsync($"/api/temporaryuploads/{token}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StoreUpload_EmptyToken_ReturnsNotFound()
    {
        // Token vazio/whitespace não corresponde à rota {token}, então retorna 404
        var dto = new
        {
            fileUrl = "/uploads/temp-file.pdf",
            fileName = "temp-file.pdf"
        };

        var response = await _client.PostAsJsonAsync("/api/temporaryuploads/   ", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StoreUpload_EmptyFileUrl_ReturnsBadRequest()
    {
        var token = Guid.NewGuid().ToString();
        var dto = new
        {
            fileUrl = "",
            fileName = "temp-file.pdf"
        };

        var response = await _client.PostAsJsonAsync($"/api/temporaryuploads/{token}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUpload_NonExistingToken_ReturnsNotFound()
    {
        var token = Guid.NewGuid().ToString();

        var response = await _client.GetAsync($"/api/temporaryuploads/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUpload_ExistingToken_ReturnsUpload()
    {
        var token = Guid.NewGuid().ToString();
        var dto = new
        {
            fileUrl = "/uploads/retrieve-me.pdf",
            fileName = "retrieve-me.pdf",
            fileType = "application/pdf",
            fileSize = 2048
        };

        // First store the upload
        await _client.PostAsJsonAsync($"/api/temporaryuploads/{token}", dto);

        // Then retrieve it
        var response = await _client.GetAsync($"/api/temporaryuploads/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("retrieve-me.pdf");
    }

    [Fact]
    public async Task CheckUpload_NonExistingToken_ReturnsNotFound()
    {
        var token = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Head, $"/api/temporaryuploads/{token}");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckUpload_ExistingToken_ReturnsOk()
    {
        var token = Guid.NewGuid().ToString();
        var dto = new
        {
            fileUrl = "/uploads/check-me.pdf",
            fileName = "check-me.pdf",
            fileType = "application/pdf",
            fileSize = 512
        };

        // First store the upload
        await _client.PostAsJsonAsync($"/api/temporaryuploads/{token}", dto);

        // Then check it
        var request = new HttpRequestMessage(HttpMethod.Head, $"/api/temporaryuploads/{token}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
