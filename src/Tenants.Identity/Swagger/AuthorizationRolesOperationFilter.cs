using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Acme.Pki.Tenants.Identity.Swagger
{
    public class AuthorizationRolesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodInfo = context.MethodInfo;
            var controllerType = methodInfo.DeclaringType;
            var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
            var isSuperAdminCreate = string.Equals(relativePath, "api/v1/superadmins", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase);

            if (controllerType == null)
            {
                return;
            }

            EnsureDefaultDocumentation(operation, context);

            var allowAnonymous = methodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
                || controllerType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

            if (allowAnonymous)
            {
                AppendRequiredAccess(operation, "No required role (anonymous access allowed).");
                return;
            }

            var authorizeAttributes = controllerType.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
                .Concat(methodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>())
                .ToList();

            if (authorizeAttributes.Count > 0)
            {
                AddBearerRequirement(operation);
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }

            var requirements = new List<string>();

            foreach (var authorize in authorizeAttributes)
            {
                if (!string.IsNullOrWhiteSpace(authorize.Roles))
                {
                    var roles = authorize.Roles
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var role in roles)
                    {
                        requirements.Add($"Role: {role}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(authorize.Policy))
                {
                    requirements.AddRange(MapPolicyToRequirements(authorize.Policy));
                }
            }

            if (isSuperAdminCreate)
            {
                AddBearerRequirement(operation);
                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "Missing or invalid bearer token after initial bootstrap is complete."
                });
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Only an authenticated SuperAdmin can create another SuperAdmin after bootstrap."
                });

                requirements.Add("Initial bootstrap: no bearer required only if no active SuperAdmin exists in the database.");
                requirements.Add("After bootstrap: bearer token is required.");
                requirements.Add("After bootstrap: only another SuperAdmin can create a SuperAdmin.");
            }

            if (requirements.Count == 0)
            {
                return;
            }

            var distinct = requirements.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            AppendRequiredAccess(operation, string.Join("; ", distinct));

            var rolesArray = new OpenApiArray();
            foreach (var requirement in distinct)
            {
                rolesArray.Add(new OpenApiString(requirement));
            }

            operation.Extensions["x-required-roles"] = rolesArray;
        }

        private static IEnumerable<string> MapPolicyToRequirements(string policy)
        {
            if (string.Equals(policy, "SuperAdminOnly", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: SuperAdmin";
                yield break;
            }

            if (string.Equals(policy, "TenantAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: AdminTenant (or SuperAdmin)";
                yield break;
            }

            if (string.Equals(policy, "RequireMfa", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Authenticated user";
                yield return "Valid MFA (claim amr=mfa) when MFA is enabled";
                yield break;
            }

            if (string.Equals(policy, "TenantOwnerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantOwner";
                yield return "TenantScope required";
                yield break;
            }

            if (string.Equals(policy, "TenantAdminSensitivePolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantAdmin";
                yield return "TenantScope required";
                yield return "MFA required";
                yield break;
            }

            if (string.Equals(policy, "TenantAdminOrUserManagerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantAdmin or UserManager";
                yield return "TenantScope required";
                yield break;
            }

            if (string.Equals(policy, "SecurityAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: SecurityAdmin";
                yield return "MFA required";
                yield break;
            }

            if (string.Equals(policy, "AppAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: AppAdmin";
                yield return "TenantScope required";
                yield return "Approval workflow required (claim approval=true)";
                yield break;
            }

            if (string.Equals(policy, "UserManagerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: UserManager";
                yield return "TenantScope required";
                yield break;
            }

            if (string.Equals(policy, "SupportAgentPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: SupportAgent";
                yield return "TenantScope required";
                yield return "Time-limited support session (claim support_session_exp)";
                yield break;
            }

            if (string.Equals(policy, "EndUserOwnResourcePolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: EndUser (or legacy User)";
                yield return "TenantScope required";
                yield return "Own resource only (route userId == sub)";
                yield break;
            }

            if (string.Equals(policy, "ServiceAccountPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: ServiceAccount";
                yield return "Restricted scope required (non-empty scope/scp and not equal to *)";
                yield break;
            }

            if (string.Equals(policy, "ViewerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: Viewer";
                yield return "TenantScope required";
                yield return "Read-only (GET/HEAD/OPTIONS)";
                yield break;
            }

            if (string.Equals(policy, "ReadOnlyAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: ReadOnlyAdmin";
                yield return "TenantScope required";
                yield return "Read-only (GET/HEAD/OPTIONS)";
                yield break;
            }

            yield return $"Policy: {policy}";
        }

        private static void AddBearerRequirement(OpenApiOperation operation)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var alreadyPresent = operation.Security.Any(requirement =>
                requirement.Keys.Any(key => key.Reference?.Id == "Bearer"));

            if (alreadyPresent)
            {
                return;
            }

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        }

        private static void AppendRequiredAccess(OpenApiOperation operation, string requiredAccess)
        {
            var line = $"Required access roles: {requiredAccess}";

            if (string.IsNullOrWhiteSpace(operation.Description))
            {
                operation.Description = line;
                return;
            }

            operation.Description += $"\n\n{line}";
        }

        private static void EnsureDefaultDocumentation(OpenApiOperation operation, OperationFilterContext context)
        {
            var httpMethod = (context.ApiDescription.HttpMethod ?? "GET").ToUpperInvariant();
            var rawPath = context.ApiDescription.RelativePath ?? string.Empty;
            var normalizedPath = rawPath.StartsWith('/') ? rawPath : $"/{rawPath}";

            operation.Summary = BuildSummary(context, httpMethod, normalizedPath);

            if (string.IsNullOrWhiteSpace(operation.Description))
            {
                operation.Description = $"Endpoint for {httpMethod} {normalizedPath}.";
            }
        }

        private static string BuildSummary(OperationFilterContext context, string httpMethod, string normalizedPath)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues.TryGetValue("controller", out var value)
                ? value
                : null;

            var action = (context.ApiDescription.ActionDescriptor as ControllerActionDescriptor)?.ActionName;

            if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(action))
            {
                return $"{controller} - {action} ({httpMethod})";
            }

            if (!string.IsNullOrWhiteSpace(controller))
            {
                return $"{controller} endpoint ({httpMethod})";
            }

            return $"{httpMethod} {normalizedPath}";
        }
    }
}