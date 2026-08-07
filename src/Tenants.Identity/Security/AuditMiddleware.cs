using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Security
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;
        private readonly IAuditService _audit;
        private readonly IIdentityTelemetry _telemetry;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, IAuditService audit, IIdentityTelemetry telemetry)
        {
            _next = next;
            _logger = logger;
            _audit = audit;
            _telemetry = telemetry;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var crudAction = MapCrudAction(context.Request.Method);
            var isApiRequest = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            var sensitive = path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/mfa", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/keys", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/tenants", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/superadmins", StringComparison.OrdinalIgnoreCase);

            if (!sensitive)
            {
                var crudOutcome = "success";
                try
                {
                    await _next(context);
                    if (context.Response.StatusCode >= 400)
                    {
                        crudOutcome = "failure";
                    }
                }
                catch
                {
                    crudOutcome = "error";
                    throw;
                }
                finally
                {
                    if (isApiRequest && !string.IsNullOrWhiteSpace(crudAction))
                    {
                        _telemetry.RecordCrudAction(crudAction!, path, crudOutcome);
                    }
                }

                return;
            }

            var userId = context.User?.FindFirst("sub")?.Value;
            var tenantId = context.User?.FindFirst("tid")?.Value ?? context.Items["TenantId"]?.ToString();
            var data = new Dictionary<string, string>
            {
                ["path"] = path,
                ["method"] = context.Request.Method,
                ["ip"] = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                ["correlationId"] = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier
            };

            try
            {
                await _next(context);
                data["outcome"] = context.Response.StatusCode < 400 ? "success" : "failure";
            }
            catch (Exception ex)
            {
                data["outcome"] = "error";
                data["details"] = ex.Message;
                _logger.LogError(ex, "AuditMiddleware caught exception");
                throw;
            }
            finally
            {
                if (isApiRequest && !string.IsNullOrWhiteSpace(crudAction))
                {
                    _telemetry.RecordCrudAction(crudAction!, path, data["outcome"]);
                }

                try
                {
                    await _audit.PublishAsync(
                        eventType: "api_call",
                        tenantId: Guid.TryParse(tenantId, out var tid) ? tid : null,
                        actorUserId: Guid.TryParse(userId, out var uid) ? uid : null,
                        data: data);
                }
                catch (Exception ex)
                {
                    _telemetry.RecordAuditPublishFailure("audit_publish_exception");
                    _logger.LogError(ex, "audit.publish.failed reason={Reason}", "audit_publish_exception");
                }
            }
        }

        private static string? MapCrudAction(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "POST" => "create",
                "GET" => "read",
                "PUT" => "update",
                "PATCH" => "update",
                "DELETE" => "delete",
                _ => null
            };
        }
    }
}