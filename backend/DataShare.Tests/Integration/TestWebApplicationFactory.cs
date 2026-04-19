using DataShare.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace DataShare.Tests.Integration;

/// <summary>
/// Factory partagée entre tous les tests d'intégration d'une même classe
/// via IClassFixture. Démarre un container Postgres dédié et applique les migrations.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("datashare_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Supprime la configuration EF Core du Program.cs pour la remplacer par Testcontainers
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Supprime le BackgroundService de purge pour éviter les effets de bord
            var hostedServices = services.Where(d =>
                d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var d in hostedServices) services.Remove(d);
        });

        builder.UseSetting("Jwt:Secret", "super-secret-test-key-with-enough-length-for-hs256-abcdef");
        builder.UseSetting("Jwt:Issuer", "DataShare.Tests");
        builder.UseSetting("Jwt:Audience", "DataShare.Tests");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
