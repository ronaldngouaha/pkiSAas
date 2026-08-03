using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Acme.Pki.Tenants.Identity.Swagger
{
    public class TestSwaggerDefaultsOperationFilter : IOperationFilter
    {
        private const string TestTenantId = "11111111-1111-1111-1111-111111111111";
        private const string TestUserId = "f044b180-5bfa-4b86-8755-d327737cdc1c";
        private const string TestSuperAdminId = "70b742e2-a8aa-4607-a3cb-913e81a07f24";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!string.Equals(context.DocumentName, "test", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

            static string NormalizePath(string path)
            {
                return path.Replace(":guid", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            bool IsPath(string expected)
            {
                return string.Equals(NormalizePath(relativePath), NormalizePath(expected), StringComparison.OrdinalIgnoreCase);
            }

            foreach (var parameter in operation.Parameters)
            {
                var name = parameter.Name.ToLowerInvariant();

                if (name == "tenantid")
                {
                    parameter.Example = new OpenApiString(TestTenantId);
                    parameter.Schema.Default = new OpenApiString(TestTenantId);
                    continue;
                }

                if (name == "userid")
                {
                    parameter.Example = new OpenApiString(TestUserId);
                    parameter.Schema.Default = new OpenApiString(TestUserId);
                    continue;
                }

                if (name == "id")
                {
                    parameter.Example = new OpenApiString(TestUserId);
                    parameter.Schema.Default = new OpenApiString(TestUserId);
                    continue;
                }

                if (name == "host")
                {
                    parameter.Example = new OpenApiString("demo.test.local");
                    parameter.Schema.Default = new OpenApiString("demo.test.local");
                    continue;
                }

                if (name == "token")
                {
                    parameter.Example = new OpenApiString("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...");
                    parameter.Schema.Default = new OpenApiString("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...");
                }
            }

            if (method == "POST" && IsPath("api/v1/auth/login"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("superadmin@pki.local"),
                    ["password"] = new OpenApiString("AdminPass123$"),
                    ["mfaCode"] = new OpenApiString("123456"),
                    ["recoveryCode"] = new OpenApiString("9FAD30E1")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["accessToken"] = new OpenApiString("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."),
                        ["refreshToken"] = new OpenApiString("refresh-token-value")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(401),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Identifiants invalides.")
                }, "401");
            }
            else if (method == "POST" && IsPath("api/v1/auth/register"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("mfa.test@pki.local"),
                    ["displayName"] = new OpenApiString("Mfa User"),
                    ["password"] = new OpenApiString("AdminPass123$"),
                    ["role"] = new OpenApiString("User")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("mfa.test@pki.local"),
                        ["normalizedEmail"] = new OpenApiString("mfa.test@pki.local"),
                        ["displayName"] = new OpenApiString("Mfa User"),
                        ["role"] = new OpenApiArray { new OpenApiString("User") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");
            }
            else if (method == "POST" && (IsPath("api/v1/auth/refresh") || IsPath("api/v1/auth/revoke")))
            {
                SetJsonExample(operation, new OpenApiString("COLLER_REFRESH_TOKEN_ICI"));

                if (IsPath("api/v1/auth/refresh"))
                {
                    SetJsonResponseExample(operation, new OpenApiObject
                    {
                        ["statuscode"] = new OpenApiInteger(200),
                        ["data"] = new OpenApiObject
                        {
                            ["accessToken"] = new OpenApiString("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."),
                            ["refreshToken"] = new OpenApiString("new-refresh-token-value")
                        },
                        ["message"] = new OpenApiString("Requete traitee avec succes.")
                    }, "200");
                }
                else
                {
                    SetJsonResponseExample(operation, new OpenApiObject
                    {
                        ["statuscode"] = new OpenApiInteger(200),
                        ["data"] = new OpenApiNull(),
                        ["message"] = new OpenApiString("Requete traitee avec succes.")
                    }, "200");
                }
            }
            else if (method == "POST" && IsPath("api/v1/mfa/{userid}/totp/verify"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["code"] = new OpenApiString("123456")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject { ["verified"] = new OpenApiBoolean(true) },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/mfa/{userid}/recovery/consume"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["code"] = new OpenApiString("9FAD30E1")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject { ["consumed"] = new OpenApiBoolean(true) },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/mfa/{userid}/totp/begin"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["manualEntryKey"] = new OpenApiString("JBSWY3DPEHPK3PXP"),
                        ["qrCodePngBase64"] = new OpenApiString("iVBORw0KGgoAAAANSUhEUgAA...")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/mfa/{userid}/recovery/generate"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiString("9FAD30E1"),
                        new OpenApiString("A8C1D4F2")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/mfa/{userid}/totp/disable"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "GET" && IsPath("api/v1/mfa/{userid}/status"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject { ["mfaEnabled"] = new OpenApiBoolean(true) },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "GET" && IsPath("api/v1/resolve"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject { ["tenantId"] = new OpenApiString(TestTenantId) },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Tenant introuvable.")
                }, "404");
            }
            else if (method == "GET" && IsPath("api/v1/auth/me"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["type"] = new OpenApiString("sub"),
                            ["value"] = new OpenApiString(TestUserId)
                        },
                        new OpenApiObject
                        {
                            ["type"] = new OpenApiString("roles"),
                            ["value"] = new OpenApiString("TenantAdmin")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/auth/introspect"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["token"] = new OpenApiString("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["userId"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.user@test.local"),
                        ["metadata"] = new OpenApiString("{\"department\":\"security\"}"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["remainingValiditySeconds"] = new OpenApiInteger(593),
                        ["expiresAtUtc"] = new OpenApiString("2026-07-30T00:10:00Z")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/domains"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["domain"] = new OpenApiString("demo.test.local"),
                    ["validationMethod"] = new OpenApiString("dns-txt")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(202),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "202");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/domains/validate"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["domain"] = new OpenApiString("demo.test.local"),
                    ["challengeResponse"] = new OpenApiString("txt-verification-value")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["validated"] = new OpenApiBoolean(true)
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Domain validation failed.")
                }, "400");
            }
            else if (method == "POST" &&
                     (IsPath("api/v1/tenants/{tenantid}/suspend") ||
                      IsPath("api/v1/tenants/{tenantid}/users/{userid}/deactivate") ||
                      IsPath("api/v1/tenants/{tenantid}/users/{userid}/reactivate")))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["reason"] = new OpenApiString("Test fonctionnel via Swagger")
                });
            }
            else if (method == "POST" && IsPath("api/v1/superadmins"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                    ["displayName"] = new OpenApiString("Bootstrap Super Admin"),
                    ["password"] = new OpenApiString("AdminPass123$")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestSuperAdminId),
                        ["tenantId"] = new OpenApiNull(),
                        ["email"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                        ["normalizedEmail"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                        ["displayName"] = new OpenApiString("Bootstrap Super Admin"),
                        ["role"] = new OpenApiArray { new OpenApiString("SuperAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Invalid request payload.")
                }, "400");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: seul un SuperAdmin peut creer un autre SuperAdmin.")
                }, "403");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(409),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("A SuperAdmin with this email already exists.")
                }, "409");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(500),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Erreur serveur: details de l'erreur")
                }, "500");
            }
            else if (method == "POST" && IsPath("api/v1/tenants"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["name"] = new OpenApiString("Demo Tenant"),
                    ["slug"] = new OpenApiString("demo-tenant"),
                    ["primaryDomain"] = new OpenApiString("demo.test.local"),
                    ["planTier"] = new OpenApiString("Standard"),
                    ["maxCertificates"] = new OpenApiInteger(100),
                    ["metadata"] = new OpenApiString("{}"),
                    ["domains"] = new OpenApiArray
                    {
                        new OpenApiString("demo.test.local")
                    }
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestTenantId),
                        ["name"] = new OpenApiString("Demo Tenant"),
                        ["slug"] = new OpenApiString("demo-tenant"),
                        ["primaryDomain"] = new OpenApiString("demo.test.local"),
                        ["createdBy"] = new OpenApiString(TestSuperAdminId),
                        ["planTier"] = new OpenApiString("Standard"),
                        ["maxCertificates"] = new OpenApiInteger(100),
                        ["metadata"] = new OpenApiString("{}"),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["isSuspended"] = new OpenApiBoolean(false),
                        ["createdAt"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["domains"] = new OpenApiArray
                        {
                            new OpenApiString("demo.test.local")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Invalid request payload.")
                }, "400");
            }
            else if (method == "GET" && IsPath("api/v1/superadmins"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString(TestSuperAdminId),
                            ["tenantId"] = new OpenApiNull(),
                            ["email"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                            ["normalizedEmail"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                            ["displayName"] = new OpenApiString("Bootstrap Super Admin"),
                            ["role"] = new OpenApiArray { new OpenApiString("SuperAdmin") },
                            ["isEmailVerified"] = new OpenApiBoolean(false),
                            ["mfaEnabled"] = new OpenApiBoolean(false),
                            ["lastLoginAt"] = new OpenApiNull(),
                            ["isActive"] = new OpenApiBoolean(true),
                            ["metadata"] = new OpenApiString("{}")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: role SuperAdmin requis.")
                }, "403");
            }
            else if (method == "GET" && IsPath("api/v1/superadmins/{id}"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestSuperAdminId),
                        ["tenantId"] = new OpenApiNull(),
                        ["email"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                        ["normalizedEmail"] = new OpenApiString("bootstrap.superadmin@pki.local"),
                        ["displayName"] = new OpenApiString("Bootstrap Super Admin"),
                        ["role"] = new OpenApiArray { new OpenApiString("SuperAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("SuperAdmin introuvable.")
                }, "404");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: role SuperAdmin requis.")
                }, "403");
            }
            else if (method == "GET" && IsPath("api/v1/superadmins/tenants/{tenantid}/users"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString(TestUserId),
                            ["tenantId"] = new OpenApiString(TestTenantId),
                            ["email"] = new OpenApiString("tenant.admin@test.local"),
                            ["normalizedEmail"] = new OpenApiString("tenant.admin@test.local"),
                            ["displayName"] = new OpenApiString("Tenant Admin"),
                            ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                            ["isEmailVerified"] = new OpenApiBoolean(false),
                            ["mfaEnabled"] = new OpenApiBoolean(false),
                            ["lastLoginAt"] = new OpenApiNull(),
                            ["isActive"] = new OpenApiBoolean(true),
                            ["metadata"] = new OpenApiString("{}")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: role SuperAdmin requis.")
                }, "403");
            }
            else if (method == "PUT" && IsPath("api/v1/superadmins/{id}"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("updated.superadmin@pki.local"),
                    ["displayName"] = new OpenApiString("Updated Super Admin"),
                    ["metadata"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestSuperAdminId),
                        ["tenantId"] = new OpenApiNull(),
                        ["email"] = new OpenApiString("updated.superadmin@pki.local"),
                        ["normalizedEmail"] = new OpenApiString("updated.superadmin@pki.local"),
                        ["displayName"] = new OpenApiString("Updated Super Admin"),
                        ["role"] = new OpenApiArray { new OpenApiString("SuperAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Invalid request payload.")
                }, "400");
            }
            else if (method == "POST" && IsPath("api/v1/superadmins/{id}/deactivate"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["reason"] = new OpenApiString("Gestion du compte superadmin en test")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/superadmins/{id}/reactivate"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["reason"] = new OpenApiString("Gestion du compte superadmin en test")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "PATCH" && IsPath("api/v1/superadmins/{id}/password"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["newPassword"] = new OpenApiString("AdminPass123$")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/superadmins/tenants/{tenantid}/users/tenant-admin"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("tenant.admin@test.local"),
                    ["displayName"] = new OpenApiString("Tenant Admin"),
                    ["password"] = new OpenApiString("AdminPass123$"),
                    ["metadata"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.admin@test.local"),
                        ["normalizedEmail"] = new OpenApiString("tenant.admin@test.local"),
                        ["displayName"] = new OpenApiString("Tenant Admin"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");
            }
            else if (method == "PATCH" && IsPath("api/v1/superadmins/tenants/{tenantid}/users/{userid}/reset-default-password"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["userId"] = new OpenApiString(TestUserId),
                        ["defaultPassword"] = new OpenApiString("TenantUser@123")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "GET" &&
                     (IsPath("api/v1/tenants/{tenantid}") ||
                      IsPath("api/v1/tenants/{tenantid}")))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestTenantId),
                        ["name"] = new OpenApiString("Demo Tenant"),
                        ["slug"] = new OpenApiString("demo-tenant"),
                        ["primaryDomain"] = new OpenApiString("demo.test.local"),
                        ["createdBy"] = new OpenApiString(TestSuperAdminId),
                        ["planTier"] = new OpenApiString("Standard"),
                        ["maxCertificates"] = new OpenApiInteger(100),
                        ["metadata"] = new OpenApiString("{}"),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["isSuspended"] = new OpenApiBoolean(false),
                        ["createdAt"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["domains"] = new OpenApiArray
                        {
                            new OpenApiString("demo.test.local")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Tenant introuvable.")
                }, "404");
            }
            else if (method == "GET" && IsPath("api/v1/tenants"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString(TestTenantId),
                            ["name"] = new OpenApiString("Demo Tenant"),
                            ["slug"] = new OpenApiString("demo-tenant"),
                            ["primaryDomain"] = new OpenApiString("demo.test.local"),
                            ["createdBy"] = new OpenApiString(TestSuperAdminId),
                            ["planTier"] = new OpenApiString("Standard"),
                            ["maxCertificates"] = new OpenApiInteger(100),
                            ["metadata"] = new OpenApiString("{}"),
                            ["isActive"] = new OpenApiBoolean(true),
                            ["isSuspended"] = new OpenApiBoolean(false),
                            ["createdAt"] = new OpenApiString("2026-07-30T00:00:00Z"),
                            ["domains"] = new OpenApiArray
                            {
                                new OpenApiString("demo.test.local")
                            }
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "PUT" && IsPath("api/v1/tenants/{tenantid}"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["name"] = new OpenApiString("Demo Tenant Updated"),
                    ["slug"] = new OpenApiString("demo-tenant"),
                    ["primaryDomain"] = new OpenApiString("demo.test.local"),
                    ["planTier"] = new OpenApiString("Standard"),
                    ["maxCertificates"] = new OpenApiInteger(250),
                    ["metadata"] = new OpenApiString("{}"),
                    ["domains"] = new OpenApiArray
                    {
                        new OpenApiString("demo.test.local")
                    }
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestTenantId),
                        ["name"] = new OpenApiString("Demo Tenant Updated"),
                        ["slug"] = new OpenApiString("demo-tenant"),
                        ["primaryDomain"] = new OpenApiString("demo.test.local"),
                        ["createdBy"] = new OpenApiString(TestSuperAdminId),
                        ["planTier"] = new OpenApiString("Standard"),
                        ["maxCertificates"] = new OpenApiInteger(250),
                        ["metadata"] = new OpenApiString("{}"),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["isSuspended"] = new OpenApiBoolean(false),
                        ["createdAt"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["domains"] = new OpenApiArray
                        {
                            new OpenApiString("demo.test.local")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Tenant introuvable.")
                }, "404");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/suspend"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestTenantId),
                        ["name"] = new OpenApiString("Demo Tenant"),
                        ["slug"] = new OpenApiString("demo-tenant"),
                        ["primaryDomain"] = new OpenApiString("demo.test.local"),
                        ["createdBy"] = new OpenApiString(TestSuperAdminId),
                        ["planTier"] = new OpenApiString("Standard"),
                        ["maxCertificates"] = new OpenApiInteger(100),
                        ["metadata"] = new OpenApiString("{}"),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["isSuspended"] = new OpenApiBoolean(true),
                        ["createdAt"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["domains"] = new OpenApiArray
                        {
                            new OpenApiString("demo.test.local")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Tenant introuvable.")
                }, "404");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/users"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("tenant.user@test.local"),
                    ["displayName"] = new OpenApiString("Tenant User"),
                    ["password"] = new OpenApiString("TenantUser@123"),
                    ["role"] = new OpenApiString("TenantAdmin"),
                    ["metadata"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.user@test.local"),
                        ["normalizedEmail"] = new OpenApiString("tenant.user@test.local"),
                        ["displayName"] = new OpenApiString("Tenant User"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");
            }
            else if (method == "GET" && IsPath("api/v1/tenants/{tenantid}/users/{userid}"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.user@test.local"),
                        ["normalizedEmail"] = new OpenApiString("tenant.user@test.local"),
                        ["displayName"] = new OpenApiString("Tenant User"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(404),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Utilisateur introuvable.")
                }, "404");
            }
            else if (method == "PUT" && IsPath("api/v1/tenants/{tenantid}/users/{userid}"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["email"] = new OpenApiString("tenant.user.updated@test.local"),
                    ["displayName"] = new OpenApiString("Tenant User Updated"),
                    ["role"] = new OpenApiString("TenantAdmin"),
                    ["metadata"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.user.updated@test.local"),
                        ["normalizedEmail"] = new OpenApiString("tenant.user.updated@test.local"),
                        ["displayName"] = new OpenApiString("Tenant User Updated"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Invalid request payload.")
                }, "400");
            }
            else if (method == "GET" && IsPath("api/v1/tenants/{tenantid}/users"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString(TestUserId),
                            ["tenantId"] = new OpenApiString(TestTenantId),
                            ["email"] = new OpenApiString("tenant.user@test.local"),
                            ["normalizedEmail"] = new OpenApiString("tenant.user@test.local"),
                            ["displayName"] = new OpenApiString("Tenant User"),
                            ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                            ["isEmailVerified"] = new OpenApiBoolean(false),
                            ["mfaEnabled"] = new OpenApiBoolean(false),
                            ["lastLoginAt"] = new OpenApiNull(),
                            ["isActive"] = new OpenApiBoolean(true),
                            ["metadata"] = new OpenApiString("{}")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/users/{userid}/deactivate"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "POST" && IsPath("api/v1/tenants/{tenantid}/users/{userid}/reactivate"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "PATCH" && IsPath("api/v1/tenants/{tenantid}/users/{userid}/password"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["newPassword"] = new OpenApiString("TenantUser@123")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "PATCH" && IsPath("api/v1/tenants/{tenantid}/users/{userid}/role"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["role"] = new OpenApiString("TenantAdmin")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString(TestUserId),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["email"] = new OpenApiString("tenant.user@test.local"),
                        ["normalizedEmail"] = new OpenApiString("tenant.user@test.local"),
                        ["displayName"] = new OpenApiString("Tenant User"),
                        ["role"] = new OpenApiArray { new OpenApiString("TenantAdmin") },
                        ["isEmailVerified"] = new OpenApiBoolean(false),
                        ["mfaEnabled"] = new OpenApiBoolean(false),
                        ["lastLoginAt"] = new OpenApiNull(),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["metadata"] = new OpenApiString("{}")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");
            }
            else if (method == "GET" && IsPath("api/v1/roles"))
            {
                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(200),
                    ["data"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString("22222222-2222-2222-2222-222222222222"),
                            ["tenantId"] = new OpenApiNull(),
                            ["name"] = new OpenApiString("TenantAdmin"),
                            ["roleMap"] = new OpenApiString("TenantAdmin"),
                            ["scope"] = new OpenApiString("global"),
                            ["definition"] = new OpenApiString("Administration du tenant"),
                            ["description"] = new OpenApiString("Role systeme pour la gestion du tenant"),
                            ["attributes"] = new OpenApiString("{}"),
                            ["isDefault"] = new OpenApiBoolean(true),
                            ["isSystem"] = new OpenApiBoolean(true),
                            ["isActive"] = new OpenApiBoolean(true),
                            ["createdAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z"),
                            ["updatedAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z")
                        }
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "200");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: tenant introuvable dans le token.")
                }, "403");
            }
            else if (method == "POST" && IsPath("api/v1/roles"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["tenantId"] = new OpenApiNull(),
                    ["name"] = new OpenApiString("CustomAuditor"),
                    ["roleMap"] = new OpenApiString("Auditor"),
                    ["scope"] = new OpenApiString("global"),
                    ["definition"] = new OpenApiString("Audit en lecture seule"),
                    ["description"] = new OpenApiString("Role global personnalise"),
                    ["attributes"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString("33333333-3333-3333-3333-333333333333"),
                        ["tenantId"] = new OpenApiNull(),
                        ["name"] = new OpenApiString("CustomAuditor"),
                        ["roleMap"] = new OpenApiString("Auditor"),
                        ["scope"] = new OpenApiString("global"),
                        ["definition"] = new OpenApiString("Audit en lecture seule"),
                        ["description"] = new OpenApiString("Role global personnalise"),
                        ["attributes"] = new OpenApiString("{}"),
                        ["isDefault"] = new OpenApiBoolean(false),
                        ["isSystem"] = new OpenApiBoolean(false),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["createdAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["updatedAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(400),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Invalid request payload.")
                }, "400");
            }
            else if (method == "POST" && IsPath("api/v1/roles/tenant"))
            {
                SetJsonExample(operation, new OpenApiObject
                {
                    ["name"] = new OpenApiString("TenantAuditor"),
                    ["roleMap"] = new OpenApiString("Auditor"),
                    ["scope"] = new OpenApiString("tenant"),
                    ["definition"] = new OpenApiString("Audit local du tenant"),
                    ["description"] = new OpenApiString("Role tenant personnalise"),
                    ["attributes"] = new OpenApiString("{}")
                });

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(201),
                    ["data"] = new OpenApiObject
                    {
                        ["id"] = new OpenApiString("44444444-4444-4444-4444-444444444444"),
                        ["tenantId"] = new OpenApiString(TestTenantId),
                        ["name"] = new OpenApiString("TenantAuditor"),
                        ["roleMap"] = new OpenApiString("Auditor"),
                        ["scope"] = new OpenApiString("tenant"),
                        ["definition"] = new OpenApiString("Audit local du tenant"),
                        ["description"] = new OpenApiString("Role tenant personnalise"),
                        ["attributes"] = new OpenApiString("{}"),
                        ["isDefault"] = new OpenApiBoolean(false),
                        ["isSystem"] = new OpenApiBoolean(false),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["createdAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z"),
                        ["updatedAtUtc"] = new OpenApiString("2026-07-30T00:00:00Z")
                    },
                    ["message"] = new OpenApiString("Requete traitee avec succes.")
                }, "201");

                SetJsonResponseExample(operation, new OpenApiObject
                {
                    ["statuscode"] = new OpenApiInteger(403),
                    ["data"] = new OpenApiNull(),
                    ["message"] = new OpenApiString("Acces refuse: tenant introuvable dans le token.")
                }, "403");
            }

        }

        private static void SetJsonExample(OpenApiOperation operation, IOpenApiAny example)
        {
            if (operation.RequestBody == null)
            {
                return;
            }

            foreach (var content in operation.RequestBody.Content)
            {
                if (content.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    content.Value.Example = example;
                }
            }
        }

        private static void SetJsonResponseExample(OpenApiOperation operation, IOpenApiAny example, params string[] statusCodes)
        {
            foreach (var response in operation.Responses)
            {
                if (statusCodes.Length > 0 && Array.IndexOf(statusCodes, response.Key) < 0)
                {
                    continue;
                }

                if (response.Value.Content == null || response.Value.Content.Count == 0)
                {
                    response.Value.Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = "object" }
                        }
                    };
                }

                foreach (var content in response.Value.Content)
                {
                    if (content.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
                    {
                        content.Value.Example = example;
                    }
                }
            }
        }
    }
}
