using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.Observability;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/observability")]
    public class ObservabilityController : ControllerBase
    {
        private readonly IIdentityTelemetry _telemetry;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        public ObservabilityController(IIdentityTelemetry telemetry, IAuditService auditService, IConfiguration configuration)
        {
            _telemetry = telemetry;
            _auditService = auditService;
            _configuration = configuration;
        }

        /// <summary>
        /// Lien pour consulter les métriques d'authentification en temps réel.
        /// </summary>
        /// <remarks>
        /// Retourne les compteurs agrégés: login success/failure, refresh failures,
        /// MFA failures, token replay attempts, key rotation failures, audit publish failures.
        /// </remarks>
        [Authorize]
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            var allowed = IsObservabilityAccessAllowed();

            if (!allowed)
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");
            }

            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;
            var requestPath = $"{Request.Scheme}://{Request.Host}{Request.Path}";

            var snapshot = _telemetry.GetSnapshot();
            var payload = new ObservabilityMetricsResponseDto
            {
                CorrelationId = correlationId,
                Links = new ObservabilityLinksDto
                {
                    Self = requestPath,
                    Rabbit = $"{Request.Scheme}://{Request.Host}/api/v1/observability/rabbit",
                    Audit = $"{Request.Scheme}://{Request.Host}/api/v1/observability/audit"
                },
                Metrics = new ObservabilityMetricsDto
                {
                    LoginSuccessTotal = snapshot.LoginSuccessTotal,
                    LoginFailureTotal = snapshot.LoginFailureTotal,
                    LoginAttemptTotal = snapshot.LoginAttemptTotal,
                    RefreshFailureTotal = snapshot.RefreshFailureTotal,
                    MfaFailureTotal = snapshot.MfaFailureTotal,
                    TokenReplayAttemptTotal = snapshot.TokenReplayAttemptTotal,
                    KeyRotationFailureTotal = snapshot.KeyRotationFailureTotal,
                    AuditPublishFailureTotal = snapshot.AuditPublishFailureTotal,
                    CrudActionTotal = snapshot.CrudActionTotal,
                    CrudCreateTotal = snapshot.CrudCreateTotal,
                    CrudReadTotal = snapshot.CrudReadTotal,
                    CrudUpdateTotal = snapshot.CrudUpdateTotal,
                    CrudDeleteTotal = snapshot.CrudDeleteTotal,
                    CrudActionsByKey = snapshot.CrudActionsByKey.ToDictionary(pair => pair.Key, pair => pair.Value),
                    LoginFailuresByReason = snapshot.LoginFailuresByReason.ToDictionary(pair => pair.Key, pair => pair.Value),
                    RefreshFailuresByReason = snapshot.RefreshFailuresByReason.ToDictionary(pair => pair.Key, pair => pair.Value),
                    MfaFailuresByReason = snapshot.MfaFailuresByReason.ToDictionary(pair => pair.Key, pair => pair.Value),
                    KeyRotationFailuresByReason = snapshot.KeyRotationFailuresByReason.ToDictionary(pair => pair.Key, pair => pair.Value),
                    AuditPublishFailuresByReason = snapshot.AuditPublishFailuresByReason.ToDictionary(pair => pair.Key, pair => pair.Value)
                }
            };

            return BuildEnvelope(StatusCodes.Status200OK, payload, "Request processed successfully.");
        }

        /// <summary>
        /// Lien pour consulter les donnees du service Audit.
        /// </summary>
        /// <remarks>
        /// Retourne les statistiques d'audit publiees par le middleware et la liste
        /// des derniers evenements observes en memoire (fenetre glissante).
        /// Filtres disponibles via query string: path, method, outcome, dateFrom, dateTo, limit.
        /// </remarks>
        [Authorize]
        [HttpGet("audit")]
        public IActionResult GetAuditServiceData(
            [FromQuery] string? path = null,
            [FromQuery] string? method = null,
            [FromQuery] string? outcome = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int? limit = null)
        {
            var allowed = IsObservabilityAccessAllowed();
            if (!allowed)
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");
            }

            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;
            var requestPath = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

            var snapshot = _auditService.GetSnapshot();
            var filteredEvents = snapshot.RecentEvents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(path))
            {
                filteredEvents = filteredEvents.Where(e =>
                    e.Data.TryGetValue("path", out var eventPath)
                    && eventPath.Contains(path, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(method))
            {
                filteredEvents = filteredEvents.Where(e =>
                    e.Data.TryGetValue("method", out var eventMethod)
                    && string.Equals(eventMethod, method, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(outcome))
            {
                filteredEvents = filteredEvents.Where(e =>
                    e.Data.TryGetValue("outcome", out var eventOutcome)
                    && string.Equals(eventOutcome, outcome, StringComparison.OrdinalIgnoreCase));
            }

            if (dateFrom.HasValue)
            {
                var fromUtc = dateFrom.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc)
                    : dateFrom.Value.ToUniversalTime();

                filteredEvents = filteredEvents.Where(e => e.OccurredAtUtc >= fromUtc);
            }

            if (dateTo.HasValue)
            {
                var toUtc = dateTo.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc)
                    : dateTo.Value.ToUniversalTime();

                filteredEvents = filteredEvents.Where(e => e.OccurredAtUtc <= toUtc);
            }

            var normalizedLimit = Math.Clamp(limit ?? 50, 1, 200);
            var selectedEvents = filteredEvents
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(normalizedLimit)
                .ToList();

            var payload = new ObservabilityAuditResponseDto
            {
                CorrelationId = correlationId,
                Links = new ObservabilityAuditLinksDto
                {
                    Self = requestPath
                },
                AuditService = new ObservabilityAuditServiceDto
                {
                    TotalPublishedEvents = snapshot.TotalPublishedEvents,
                    FilteredEventsCount = selectedEvents.Count,
                    LastPublishedAtUtc = snapshot.LastPublishedAtUtc,
                    RecentEvents = selectedEvents
                        .Select(e => new ObservabilityAuditEventDto
                        {
                            EventType = e.EventType,
                            TenantId = e.TenantId,
                            ActorUserId = e.ActorUserId,
                            OccurredAtUtc = e.OccurredAtUtc,
                            Data = e.Data.ToDictionary(pair => pair.Key, pair => pair.Value)
                        })
                        .ToList()
                }
            };

            return BuildEnvelope(StatusCodes.Status200OK, payload, "Request processed successfully.");
        }

        /// <summary>
        /// Lien pour consulter les donnees du service RabbitMQ.
        /// </summary>
        /// <remarks>
        /// Retourne l'etat de connectivite TCP et la configuration active du broker RabbitMQ
        /// (host, port, vhost, queue, exchange) utilisee par le service d'audit.
        /// </remarks>
        [Authorize]
        [HttpGet("rabbit")]
        public async Task<IActionResult> GetRabbitServiceData()
        {
            var allowed = IsObservabilityAccessAllowed();
            if (!allowed)
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");
            }

            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;
            var requestPath = $"{Request.Scheme}://{Request.Host}{Request.Path}";

            var host = _configuration["RabbitMq:Host"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMq:Port"], out var parsedPort) ? parsedPort : 5672;
            var virtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/";
            var queue = _configuration["RabbitMq:Queue"] ?? "audit.events";
            var exchange = _configuration["RabbitMq:Exchange"] ?? "audit.exchange";

            var tcpReachable = await CanReachRabbitTcpAsync(host, port);
            var snapshot = _telemetry.GetSnapshot();

            var payload = new ObservabilityRabbitResponseDto
            {
                CorrelationId = correlationId,
                Links = new ObservabilityRabbitLinksDto
                {
                    Self = requestPath
                },
                RabbitService = new ObservabilityRabbitServiceDto
                {
                    Status = tcpReachable ? "up" : "down",
                    Host = host,
                    Port = port,
                    VirtualHost = virtualHost,
                    Queue = queue,
                    Exchange = exchange,
                    TcpReachable = tcpReachable,
                    AuditPublishFailureTotal = snapshot.AuditPublishFailureTotal
                }
            };

            return BuildEnvelope(StatusCodes.Status200OK, payload, "Request processed successfully.");
        }

        private bool IsObservabilityAccessAllowed()
        {
            return User.Claims.Any(c => c.Type == "roles" &&
                                        (string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(c.Value, "SecurityAdmin", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(c.Value, "TenantAdmin", StringComparison.OrdinalIgnoreCase)));
        }

        private static async Task<bool> CanReachRabbitTcpAsync(string host, int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != connectTask)
                {
                    return false;
                }

                await connectTask;
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private ObjectResult BuildEnvelope(int statusCode, object? data, string message)
        {
            return StatusCode(statusCode, new
            {
                statuscode = statusCode,
                data,
                message
            });
        }
    }
}
