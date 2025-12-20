using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Helpers;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<string> GetAccessTokenAsync(
        this HttpClient client,
        IServiceProvider serviceProvider,
        string email,
        string password = "Test@123")
    {
        var loginRequest = new { Email = email, Password = password, RememberMe = false };
        var response = await client.PostAsJsonAsync("/api/Auth/login", loginRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to login: {await response.Content.ReadAsStringAsync()}");
        }
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static void SetBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<T?> GetFromJsonSafeAsync<T>(this HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public static async Task AuthenticateAsAdminAsync(this HttpClient client, IServiceProvider serviceProvider)
    {
        // Primeiro, cria o usuário admin se não existir
        await TestDataSeeder.CreateAdminUserAsync(serviceProvider);
        
        // Faz login - precisamos usar a senha real que foi hashada
        using var scope = serviceProvider.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        
        // Gera um token diretamente
        var token = jwtService.GenerateAccessToken(
            Guid.NewGuid(), 
            "admin@test.com", 
            "ADMIN");
        
        client.SetBearerToken(token);
    }

    public static async Task AuthenticateAsProfessionalAsync(this HttpClient client, IServiceProvider serviceProvider)
    {
        await TestDataSeeder.CreateProfessionalUserAsync(serviceProvider);
        
        using var scope = serviceProvider.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        
        var token = jwtService.GenerateAccessToken(
            Guid.NewGuid(),
            "professional@test.com",
            "PROFESSIONAL");
        
        client.SetBearerToken(token);
    }

    public static async Task AuthenticateAsPatientAsync(this HttpClient client, IServiceProvider serviceProvider)
    {
        await TestDataSeeder.CreatePatientUserAsync(serviceProvider);
        
        using var scope = serviceProvider.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        
        var token = jwtService.GenerateAccessToken(
            Guid.NewGuid(),
            "patient@test.com",
            "PATIENT");
        
        client.SetBearerToken(token);
    }
}
