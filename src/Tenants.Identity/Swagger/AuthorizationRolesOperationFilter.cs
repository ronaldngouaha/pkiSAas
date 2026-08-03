using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
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

            var allowAnonymous = methodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
                || controllerType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

            if (allowAnonymous)
            {
                AppendRequiredAccess(operation, "Aucun role obligatoire (acces anonyme autorise).");
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
                    Description = "Bearer absent ou invalide quand le bootstrap initial est deja termine."
                });
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Seul un SuperAdmin authentifie peut creer un autre SuperAdmin apres le bootstrap."
                });

                requirements.Add("Bootstrap initial: aucun bearer requis seulement si aucun SuperAdmin actif n'existe en base.");
                requirements.Add("Apres bootstrap: bearer token obligatoire.");
                requirements.Add("Apres bootstrap: seul un autre SuperAdmin peut creer un SuperAdmin.");
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
                yield return "Role: AdminTenant (ou SuperAdmin)";
                yield break;
            }

            if (string.Equals(policy, "RequireMfa", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Utilisateur authentifie";
                yield return "MFA valide (claim amr=mfa) quand MFA est active";
                yield break;
            }

            if (string.Equals(policy, "TenantOwnerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantOwner";
                yield return "TenantScope requis";
                yield break;
            }

            if (string.Equals(policy, "TenantAdminSensitivePolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantAdmin";
                yield return "TenantScope requis";
                yield return "MFA requis";
                yield break;
            }

            if (string.Equals(policy, "TenantAdminOrUserManagerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: TenantAdmin ou UserManager";
                yield return "TenantScope requis";
                yield break;
            }

            if (string.Equals(policy, "SecurityAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: SecurityAdmin";
                yield return "MFA requis";
                yield break;
            }

            if (string.Equals(policy, "AppAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: AppAdmin";
                yield return "TenantScope requis";
                yield return "Approval workflow requis (claim approval=true)";
                yield break;
            }

            if (string.Equals(policy, "UserManagerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: UserManager";
                yield return "TenantScope requis";
                yield break;
            }

            if (string.Equals(policy, "SupportAgentPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: SupportAgent";
                yield return "TenantScope requis";
                yield return "Session support limitee dans le temps (claim support_session_exp)";
                yield break;
            }

            if (string.Equals(policy, "EndUserOwnResourcePolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: EndUser (ou User legacy)";
                yield return "TenantScope requis";
                yield return "Ressource propre uniquement (userId route == sub)";
                yield break;
            }

            if (string.Equals(policy, "ServiceAccountPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: ServiceAccount";
                yield return "Scope restreint requis (scope/scp non vide et different de *)";
                yield break;
            }

            if (string.Equals(policy, "ViewerPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: Viewer";
                yield return "TenantScope requis";
                yield return "Lecture seule (GET/HEAD/OPTIONS)";
                yield break;
            }

            if (string.Equals(policy, "ReadOnlyAdminPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Role: ReadOnlyAdmin";
                yield return "TenantScope requis";
                yield return "Lecture seule (GET/HEAD/OPTIONS)";
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
            var line = $"Roles/Acces requis: {requiredAccess}";

            if (string.IsNullOrWhiteSpace(operation.Description))
            {
                operation.Description = line;
                return;
            }

            operation.Description += $"\n\n{line}";
        }
    }
}