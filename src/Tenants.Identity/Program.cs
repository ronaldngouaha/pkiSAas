using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Acme.Pki.Tenants.Identity.Options;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("Default") ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
builder.Services.AddDbContext<TenantsIdentityDbContext>(options => options.UseSqlServer(conn));

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<IKeyProvider, VaultKeyProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["JWT_ISSUER"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["JWT_AUDIENCE"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
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
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireClaim("roles", "SuperAdmin"));
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();