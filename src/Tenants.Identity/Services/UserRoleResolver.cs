using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Acme.Pki.Tenants.Identity.Models;

namespace Acme.Pki.Tenants.Identity.Services
{
    internal static class UserRoleResolver
    {
        public static IReadOnlyList<TenantRole> GetRoles(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (user.TenantId == null || user.Role == TenantRole.SuperAdmin)
            {
                return new[] { TenantRole.SuperAdmin };
            }

            var roles = new List<TenantRole>();
            var metadata = ParseMetadata(user.Metadata);

            if (metadata["roles"] is JsonArray roleArray)
            {
                foreach (var roleNode in roleArray)
                {
                    var roleName = roleNode?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(roleName))
                    {
                        continue;
                    }

                    if (TryParseTenantRole(roleName, out var parsed) && parsed != TenantRole.SuperAdmin)
                    {
                        roles.Add(parsed);
                    }
                }
            }

            if (roles.Count == 0 && user.Role != TenantRole.SuperAdmin)
            {
                roles.Add(user.Role);
            }

            roles = roles.Where(r => r != TenantRole.SuperAdmin).Distinct().ToList();
            if (roles.Count == 0)
            {
                roles.Add(TenantRole.User);
            }

            return roles;
        }

        public static List<TenantRole> ParseRequestedRoles(IEnumerable<string> roleNames)
        {
            var parsedRoles = new List<TenantRole>();

            if (roleNames != null)
            {
                foreach (var roleName in roleNames)
                {
                    if (!TryParseTenantRole(roleName, out var parsed))
                    {
                        continue;
                    }

                    if (parsed == TenantRole.SuperAdmin)
                    {
                        continue;
                    }

                    parsedRoles.Add(parsed);
                }
            }

            parsedRoles = parsedRoles.Distinct().ToList();
            if (parsedRoles.Count == 0)
            {
                parsedRoles.Add(TenantRole.User);
            }

            return parsedRoles;
        }

        public static void SetRoles(User user, IEnumerable<TenantRole> roles)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (user.TenantId == null)
            {
                user.Role = TenantRole.SuperAdmin;
                user.ServiceAccount = false;
                return;
            }

            var roleList = (roles ?? Array.Empty<TenantRole>())
                .Where(r => r != TenantRole.SuperAdmin)
                .Distinct()
                .ToList();

            if (roleList.Count == 0)
            {
                roleList.Add(TenantRole.User);
            }

            user.Role = ResolvePrimaryRole(roleList);
            user.ServiceAccount = roleList.Contains(TenantRole.ServiceAccount);

            var metadata = ParseMetadata(user.Metadata);
            metadata["roles"] = new JsonArray(roleList.Select(r => JsonValue.Create(r.ToString())).ToArray());
            user.Metadata = metadata.ToJsonString();
        }

        public static bool TryParseTenantRole(string roleName, out TenantRole role)
        {
            if (Enum.TryParse<TenantRole>(roleName, true, out role))
            {
                return true;
            }

            role = TenantRole.User;
            return false;
        }

        public static string BuildPublicMetadata(string metadata)
        {
            var parsed = ParseMetadata(metadata);
            parsed.Remove("roles");
            return parsed.ToJsonString();
        }

        private static TenantRole ResolvePrimaryRole(IReadOnlyCollection<TenantRole> roles)
        {
            if (roles.Contains(TenantRole.TenantOwner)) return TenantRole.TenantOwner;
            if (roles.Contains(TenantRole.TenantAdmin)) return TenantRole.TenantAdmin;
            if (roles.Contains(TenantRole.SecurityAdmin)) return TenantRole.SecurityAdmin;
            if (roles.Contains(TenantRole.AppAdmin)) return TenantRole.AppAdmin;
            if (roles.Contains(TenantRole.UserManager)) return TenantRole.UserManager;
            if (roles.Contains(TenantRole.SupportAgent)) return TenantRole.SupportAgent;
            if (roles.Contains(TenantRole.ServiceAccount)) return TenantRole.ServiceAccount;
            if (roles.Contains(TenantRole.ReadOnlyAdmin)) return TenantRole.ReadOnlyAdmin;
            if (roles.Contains(TenantRole.Viewer)) return TenantRole.Viewer;
            if (roles.Contains(TenantRole.EndUser)) return TenantRole.EndUser;
            return TenantRole.User;
        }

        private static JsonObject ParseMetadata(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return new JsonObject();
            }

            try
            {
                var node = JsonNode.Parse(metadata);
                return node as JsonObject ?? new JsonObject();
            }
            catch
            {
                return new JsonObject();
            }
        }
    }
}
