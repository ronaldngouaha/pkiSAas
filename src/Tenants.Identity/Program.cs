using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Acme.Pki.Tenants.Identity.Options;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.DTOs;

var builder = WebApplication.CreateBuilder(args);

string? ResolveConfigValue(string configKey, string envKey)
{
    var fromConfig = builder.Configuration[configKey];
    if (!string.IsNullOrWhiteSpace(fromConfig) && !(fromConfig.StartsWith("${") && fromConfig.EndsWith("}")))
    {
        return fromConfig;
    }

    var fromEnv = builder.Configuration[envKey] ?? Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrWhiteSpace(fromEnv) && !(fromEnv.StartsWith("${") && fromEnv.EndsWith("}")))
    {
        return fromEnv;
    }

    return null;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var currentEnvironment = builder.Environment.EnvironmentName;

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("test", new OpenApiInfo
    {
        Title = "Acme PKI Tenants Identity API - Test",
        Version = "v1",
        Description = "Documentation Swagger pour l'environnement de test (base de donnees Test)."
    });

    options.SwaggerDoc("live", new OpenApiInfo
    {
        Title = "Acme PKI Tenants Identity API - Live",
        Version = "v1",
        Description = "Documentation Swagger pour l'environnement live (base de donnees Live)."
    });

    options.DocInclusionPredicate((_, _) => true);
});

string ResolveConnectionString()
{
    static string ResolvePlaceholders(string connectionString)
    {
        var sqlPassword = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (!string.IsNullOrWhiteSpace(sqlPassword))
        {
            connectionString = connectionString.Replace("${MSSQL_SA_PASSWORD}", sqlPassword, StringComparison.Ordinal);
        }

        return connectionString;
    }

    if (builder.Environment.IsEnvironment("Test"))
    {
        var testConn = builder.Configuration.GetConnectionString("Test")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Test")
            ?? builder.Configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("Missing Test connection string.");

        return ResolvePlaceholders(testConn);
    }

    if (builder.Environment.IsEnvironment("Live"))
    {
        var liveConn = builder.Configuration.GetConnectionString("Live")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Live")
            ?? builder.Configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("Missing Live connection string.");

        return ResolvePlaceholders(liveConn);
    }

    var defaultConn = builder.Configuration.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? throw new InvalidOperationException("Missing Default connection string.");

    return ResolvePlaceholders(defaultConn);
}

var conn = ResolveConnectionString();
builder.Services.AddDbContext<TenantsIdentityDbContext>(options => options.UseSqlServer(conn));

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDomainService, DomainService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IKeyEncryptionService, KeyEncryptionService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<IKeyProvider, VaultKeyProvider>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var issuer = ResolveConfigValue("Jwt:Issuer", "JWT_ISSUER") ?? "Acme.Pki.Tenants.Identity";
        var audience = ResolveConfigValue("Jwt:Audience", "JWT_AUDIENCE") ?? "Acme.Pki";

        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "roles",
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var keyProvider = builder.Services.BuildServiceProvider().GetRequiredService<IKeyProvider>();
                var jwks = keyProvider.GetPublicJwksAsync().GetAwaiter().GetResult();
                return new JsonWebKeySet(jwks).GetSigningKeys();
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
});

builder.Services.AddHealthChecks().AddDbContextCheck<TenantsIdentityDbContext>();

var app = builder.Build();

var shouldSeedSuperAdmin = string.Equals(
    Environment.GetEnvironmentVariable("SEED_SUPERADMIN"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (shouldSeedSuperAdmin)
{
    var seedEmail = Environment.GetEnvironmentVariable("SEED_SUPERADMIN_EMAIL");
    var seedPassword = Environment.GetEnvironmentVariable("SEED_SUPERADMIN_PASSWORD");

    if (string.IsNullOrWhiteSpace(seedEmail) || string.IsNullOrWhiteSpace(seedPassword))
    {
        app.Logger.LogWarning(
            "SEED_SUPERADMIN=true mais SEED_SUPERADMIN_EMAIL/SEED_SUPERADMIN_PASSWORD sont absents. Seed ignoré.");
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        try
        {
            await authService.SeedSuperAdminAsync(new RegisterRequestDto
            {
                Email = seedEmail,
                DisplayName = "Super Admin",
                Password = seedPassword,
                Role = "SuperAdmin"
            });

            app.Logger.LogInformation(
                "Provisioning SuperAdmin exécuté pour {Email}. Utiliser un script sécurisé au lieu des variables d'environnement en production.",
                seedEmail);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Échec du provisioning SuperAdmin au démarrage.");
            throw;
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/test/swagger.json", "Tenants.Identity API Test v1");
    options.SwaggerEndpoint("/swagger/live/swagger.json", "Tenants.Identity API Live v1");
});

app.Logger.LogInformation("Environment: {EnvironmentName}", currentEnvironment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();