using Acme.Pki.Tenants.Identity.Security.Handlers;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Pki.Tenants.Identity.Security
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddTenantAuthorization(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthorizationHandler, TenantAdminHandler>();
            services.AddScoped<IAuthorizationHandler, MfaHandler>();
            services.AddScoped<IAuthorizationHandler, TenantScopeHandler>();
            services.AddScoped<IAuthorizationHandler, OwnResourceHandler>();
            services.AddScoped<IAuthorizationHandler, ReadOnlyHandler>();
            services.AddScoped<IAuthorizationHandler, ApprovalWorkflowHandler>();
            services.AddScoped<IAuthorizationHandler, RateLimitHandler>();
            services.AddScoped<IAuthorizationHandler, ScopeRestrictedHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("TenantAdminPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new TenantAdminRequirement());
                });

                options.AddPolicy("RequireMfa", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new MfaRequirement());
                });

                options.AddPolicy("SuperAdminOnly", policy =>
                {
                    policy.RequireClaim("roles", "SuperAdmin");
                });

                options.AddPolicy("TenantOwnerPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("TenantOwner");
                    policy.Requirements.Add(new TenantScopeRequirement());
                });

                options.AddPolicy("TenantAdminSensitivePolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("TenantAdmin");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new MfaRequirement());
                });

                options.AddPolicy("TenantAdminOrUserManagerPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("TenantAdmin", "UserManager");
                    policy.Requirements.Add(new TenantScopeRequirement());
                });

                options.AddPolicy("SecurityAdminPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("SecurityAdmin");
                    policy.Requirements.Add(new MfaRequirement());
                });

                options.AddPolicy("AppAdminPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("AppAdmin");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new ApprovalWorkflowRequirement());
                });

                options.AddPolicy("UserManagerPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("UserManager");
                    policy.Requirements.Add(new TenantScopeRequirement());
                });

                options.AddPolicy("SupportAgentPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("SupportAgent");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new RateLimitRequirement());
                });

                options.AddPolicy("EndUserOwnResourcePolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("EndUser", "User");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new OwnResourceRequirement());
                });

                options.AddPolicy("ServiceAccountPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("ServiceAccount");
                    policy.Requirements.Add(new ScopeRestrictedRequirement());
                });

                options.AddPolicy("ViewerPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Viewer");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new ReadOnlyRequirement());
                });

                options.AddPolicy("ReadOnlyAdminPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("ReadOnlyAdmin");
                    policy.Requirements.Add(new TenantScopeRequirement());
                    policy.Requirements.Add(new ReadOnlyRequirement());
                });
            });

            return services;
        }
    }
}