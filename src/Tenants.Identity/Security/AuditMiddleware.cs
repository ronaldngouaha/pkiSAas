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

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, IAuditService audit)
        {
            _next = next;
            _logger = logger;
            _audit = audit;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var sensitive = path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/mfa", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/keys", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/tenants", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/superadmins", StringComparison.OrdinalIgnoreCase);

            if (!sensitive)
            {
                await _next(context);
                return;
            }

            var userId = context.User?.FindFirst("sub")?.Value;
            var tenantId = context.User?.FindFirst("tid")?.Value ?? context.Items["TenantId"]?.ToString();
            var data = new Dictionary<string, string>
            {
                ["path"] = path,
                ["method"] = context.Request.Method,
                ["ip"] = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty
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
                _ = _audit.PublishAsync(
                    eventType: "api_call",
                    tenantId: Guid.TryParse(tenantId, out var tid) ? tid : null,
                    actorUserId: Guid.TryParse(userId, out var uid) ? uid : null,
                    data: data);
            }
        }
    }
}