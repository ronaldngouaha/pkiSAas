using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Security
{
    public class TenantScopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantScopeMiddleware> _logger;

        public TenantScopeMiddleware(RequestDelegate next, ILogger<TenantScopeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var tidClaim = context.User.FindFirst("tid")?.Value;
                    if (!string.IsNullOrWhiteSpace(tidClaim) && Guid.TryParse(tidClaim, out var tid))
                    {
                        context.Items["TenantId"] = tid;
                    }
                }

                if (!context.Items.ContainsKey("TenantId"))
                {
                    var host = context.Request.Host.Host;
                    var parts = host.Split('.');
                    if (parts.Length > 2)
                    {
                        context.Items["TenantSlug"] = parts[0];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TenantScopeMiddleware failed to resolve tenant");
            }

            await _next(context);
        }
    }
}
