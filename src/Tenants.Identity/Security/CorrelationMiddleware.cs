using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Security
{
    public class CorrelationMiddleware
    {
        private const string CorrelationHeader = "X-Correlation-Id";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var incomingCorrelationId = context.Request.Headers[CorrelationHeader].ToString();
            var correlationId = string.IsNullOrWhiteSpace(incomingCorrelationId)
                ? context.TraceIdentifier
                : incomingCorrelationId.Trim();

            context.TraceIdentifier = correlationId;
            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers[CorrelationHeader] = correlationId;

            Activity.Current?.SetTag("correlationId", correlationId);

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }
    }
}
