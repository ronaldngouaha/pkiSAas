using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DnsClient;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Acme.Pki.Tenants.Identity.Options;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.Security;
using Acme.Pki.Tenants.Identity.Swagger;
using Acme.Pki.Tenants.Identity.Workers;
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
            requestPath.StartsWith("/api/v1/observability", StringComparison.OrdinalIgnoreCase) ||
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
        Description = "Swagger documentation for the test environment (Test database). The first SuperAdmin can be created without a bearer token only when no active SuperAdmin exists in the database; afterward, only a bearer token from another SuperAdmin is accepted."
    });

    options.SwaggerDoc("live", new OpenApiInfo
    {
        Title = "Acme PKI Tenants Identity API - Live",
        Version = "v1",
        Description = "Swagger documentation for the live environment (Live database). The first SuperAdmin can be created without a bearer token only when no active SuperAdmin exists in the database; afterward, only a bearer token from another SuperAdmin is accepted."
    });

    options.DocInclusionPredicate((_, _) => true);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your_jwt_token}"
    });

    options.OperationFilter<TestSwaggerDefaultsOperationFilter>();
    options.OperationFilter<AuthorizationRolesOperationFilter>();

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
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
builder.Services.AddSingleton(new LookupClient());
builder.Services.AddHttpClient();

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IRoleCatalogService, RoleCatalogService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IKeyEncryptionService, KeyEncryptionService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddSingleton<IIdentityTelemetry, IdentityTelemetry>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<IKeyProvider, VaultKeyProvider>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ActiveUserTokenValidator>();
builder.Services.AddScoped<IDomainService>(sp =>
{
    var dns = sp.GetRequiredService<LookupClient>();
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var http = httpFactory.CreateClient();
    var db = sp.GetRequiredService<TenantsIdentityDbContext>();
    var logger = sp.GetRequiredService<ILogger<DomainService>>();
    var auditService = sp.GetRequiredService<IAuditService>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new DomainService(db, logger, dns, http, auditService, configuration);
});
builder.Services.AddHostedService<DomainValidationWorker>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
    new ConfigureNamedOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var keyProvider = sp.GetRequiredService<IKeyProvider>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        options.TokenValidationParameters ??= new TokenValidationParameters();
        options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
        {
            try
            {
                var jwks = keyProvider.GetPublicJwksAsync().GetAwaiter().GetResult();
                var keys = new JsonWebKeySet(jwks).GetSigningKeys();
                if (keys.Count > 0)
                {
                    return keys;
                }
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("JwtAuth").LogWarning(ex, "auth.jwt.key_resolver.jwks_failed kid={Kid}", kid ?? string.Empty);
            }

            var (fallbackKid, fallbackPrivateKey) = keyProvider.GetActiveRsaKeyAsync().GetAwaiter().GetResult();
            using var rsa = RSA.Create();
            rsa.ImportParameters(fallbackPrivateKey);

            return new SecurityKey[]
            {
                new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = fallbackKid }
            };
        };
    }));

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
        options.TokenValidationParameters ??= new TokenValidationParameters();
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidIssuer = issuer;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudience = audience;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Sub;
        options.TokenValidationParameters.RoleClaimType = "roles";

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    return context.Response.WriteAsJsonAsync(new ApiEnvelopeDto
                    {
                        statuscode = StatusCodes.Status401Unauthorized,
                        data = null,
                        message = "Unauthorized"
                    });
                }

                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    return context.Response.WriteAsJsonAsync(new ApiEnvelopeDto
                    {
                        statuscode = StatusCodes.Status403Forbidden,
                        data = null,
                        message = "Access denied."
                    });
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("JwtAuth");
                    logger.LogWarning(context.Exception, "auth.jwt.failed path={Path} reason={Reason}", context.HttpContext.Request.Path, context.Exception.Message);
                }

                return Task.CompletedTask;
            },
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
app.UseMiddleware<CorrelationMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantScopeMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();