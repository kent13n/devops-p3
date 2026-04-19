using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using DataShare.Api.Endpoints;
using Microsoft.AspNetCore.RateLimiting;
using DataShare.Application.Interfaces;
using DataShare.Application.Services;
using DataShare.Infrastructure.Data;
using DataShare.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Limites de taille pour l'upload (1 Go + marge)
    builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_100_000_000);
    builder.Services.Configure<FormOptions>(o =>
    {
        o.MultipartBodyLengthLimit = 1_100_000_000;
        o.ValueLengthLimit = 1_048_576;
    });

    // Base de données
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IApplicationDbContext>(sp =>
        sp.GetRequiredService<ApplicationDbContext>());

    // ASP.NET Identity
    builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // JWT Bearer
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret non configuré");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

    builder.Services.AddAuthorization();

    // Rate limiting partitionné par IP
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.AddPolicy("upload", http =>
            RateLimitPartition.GetFixedWindowLimiter(
                (http.Connection.RemoteIpAddress?.IsIPv4MappedToIPv6 == true
                    ? http.Connection.RemoteIpAddress.MapToIPv4()
                    : http.Connection.RemoteIpAddress)?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));
        options.AddPolicy("download", http =>
            RateLimitPartition.GetFixedWindowLimiter(
                (http.Connection.RemoteIpAddress?.IsIPv4MappedToIPv6 == true
                    ? http.Connection.RemoteIpAddress.MapToIPv4()
                    : http.Connection.RemoteIpAddress)?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // Services Infrastructure
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
    builder.Services.AddSingleton<IFilePasswordHasher, FilePasswordHasher>();

    // Services Application
    builder.Services.AddScoped<FileUploadService>();
    builder.Services.AddScoped<FileDownloadService>();
    builder.Services.AddScoped<FileDeleteService>();
    builder.Services.AddScoped<FileListService>();

    // BackgroundService de purge
    builder.Services.AddHostedService<ExpiredFilesCleanupService>();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Forwarded Headers (reverse proxy nginx en Docker)
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto
                         | ForwardedHeaders.XForwardedHost
    };
    // Accepter les proxies du réseau Docker bridge (172.16.0.0/12)
    // Réseaux privés : bridge Docker par défaut + networks custom + déploiements non-Docker
    forwardedOptions.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    forwardedOptions.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    forwardedOptions.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
    app.UseForwardedHeaders(forwardedOptions);

    // Header de sécurité
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });

    // Migration automatique si configuré
    var autoMigrate = app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("DATASHARE_AUTO_MIGRATE");
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Endpoints
    app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
       .WithTags("Health");

    app.MapAuthEndpoints();
    app.MapFileEndpoints();
    app.MapDownloadEndpoints();
    app.MapTagEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application a échoué au démarrage");
}
finally
{
    Log.CloseAndFlush();
}
