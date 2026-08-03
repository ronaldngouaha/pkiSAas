using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Acme.Pki.Tenants.Identity.Options;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.Security;
using Acme.Pki.Tenants.Identity.Swagger;
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
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var requestPath = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (requestPath.StartsWith("/api/v1/superadmins", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/api/v1/tenants", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/api/v1/roles", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/api/v1/mfa", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/api/v1/resolve", StringComparison.OrdinalIgnoreCase))
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors?.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(err => string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Invalid request payload." : err.ErrorMessage))
                .Distinct()
                .ToArray();

            var message = errors.Length > 0
                ? string.Join("; ", errors)
                : "Invalid request payload.";

            return new BadRequestObjectResult(new
            {
                statuscode = StatusCodes.Status400BadRequest,
                data = (object?)null,
                message
            });
        }

        return new BadRequestObjectResult(context.ModelState);
    };
});
builder.Services.AddEndpointsApiExplorer();

var currentEnvironment = builder.Environment.EnvironmentName;

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("test", new OpenApiInfo
    {
        Title = "Acme PKI Tenants Identity API - Test",
        Version = "v1",
        Description = "Documentation Swagger pour l'environnement de test (base de donnees Test). La creation du premier SuperAdmin peut se faire sans bearer tant qu'aucun SuperAdmin actif n'existe en base; ensuite seul un bearer d'un autre SuperAdmin est accepte."
    });

    options.SwaggerDoc("live", new OpenApiInfo
    {
        Title = "Acme PKI Tenants Identity API - Live",
        Version = "v1",
        Description = "Documentation Swagger pour l'environnement live (base de donnees Live). La creation du premier SuperAdmin peut se faire sans bearer tant qu'aucun SuperAdmin actif n'existe en base; ensuite seul un bearer d'un autre SuperAdmin est accepte."
    });

    options.DocInclusionPredicate((_, _) => true);

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrer: Bearer {votre_token_jwt}"
    });

    options.OperationFilter<TestSwaggerDefaultsOperationFilter>();
    options.OperationFilter<AuthorizationRolesOperationFilter>();
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
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IRoleCatalogService, RoleCatalogService>();
builder.Services.AddScoped<IDomainService, DomainService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IKeyEncryptionService, KeyEncryptionService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<IKeyProvider, VaultKeyProvider>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ActiveUserTokenValidator>();

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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<ActiveUserTokenValidator>();
                var isActive = await validator.IsActiveAsync(context.Principal);

                if (!isActive)
                {
                    context.Fail("Unauthorized");
                }
            }
        };
    });

builder.Services.AddTenantAuthorization();

builder.Services.AddHealthChecks().AddDbContextCheck<TenantsIdentityDbContext>();

var app = builder.Build();

using (var bootstrapScope = app.Services.CreateScope())
{
    var roleCatalogService = bootstrapScope.ServiceProvider.GetRequiredService<IRoleCatalogService>();
    await roleCatalogService.SeedDefaultsAsync();
}

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
app.UseMiddleware<TenantScopeMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();