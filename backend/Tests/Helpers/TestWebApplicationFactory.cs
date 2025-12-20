using Application.DTOs.Email;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Helpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configura variáveis de ambiente necessárias para o JWT
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "TestSecretKeyForIntegrationTestingThatIsLongEnough123!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES", "60");
        Environment.SetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRATION_DAYS", "7");

        builder.ConfigureServices(services =>
        {
            // Remove o contexto de banco de dados existente
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove qualquer DbContextOptions existente
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Cria uma conexão SQLite em memória que persiste durante os testes
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Adiciona um banco de dados SQLite em memória para testes
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Configura EmailSettings para testes
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(EmailSettings));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            services.AddSingleton(new EmailSettings { Enabled = false });

            // Build do service provider
            var sp = services.BuildServiceProvider();

            // Cria o scope e inicializa o banco de dados
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                // Cria as tabelas
                db.Database.EnsureCreated();
            }
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
