using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs.Domain;
using Acme.Pki.Tenants.Identity.Models;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class DomainService : IDomainService
    {
        private static readonly ConcurrentDictionary<Guid, DateTimeOffset> ValidationAttempts = new();
        private readonly TenantsIdentityDbContext _db;
        private readonly ILogger<DomainService> _logger;
        private readonly LookupClient _dnsClient;
        private readonly HttpClient _http;
        private readonly IAuditService _auditService;
        private readonly TimeSpan _validationCooldown;

        public DomainService(TenantsIdentityDbContext db, ILogger<DomainService> logger, LookupClient dnsClient, HttpClient http, IAuditService auditService, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _dnsClient = dnsClient;
            _http = http;
            _auditService = auditService;
            var cooldownMinutes = configuration.GetValue<int?>("DomainValidation:ValidateCooldownMinutes") ?? 15;
            _validationCooldown = TimeSpan.FromMinutes(Math.Max(1, cooldownMinutes));
        }

        public async Task<TenantDomainDto> AddDomainAsync(Guid tenantId, TenantDomainCreateDto dto)
        {
            var domainNormalized = dto.Domain.Trim().ToLowerInvariant();

            // check uniqueness for tenant
            var exists = await _db.TenantDomains.AnyAsync(d => d.TenantId == tenantId && d.Domain == domainNormalized);
            if (exists) throw new InvalidOperationException("Domain already added for this tenant.");

            var challenge = GenerateChallengeToken();
            var td = new TenantDomain
            {
                TenantId = tenantId,
                Domain = domainNormalized,
                IsValidated = false,
                ValidationMethod = dto.ValidationMethod ?? "dns",
                CreatedAt = DateTime.UtcNow
            };

            // store challenge in Metadata or separate column; here we use Metadata in TenantDomain (add property if needed)
            // For simplicity, store challenge in a new column "ValidationToken" if model has it; otherwise use Metadata
            // We'll use Metadata field on TenantDomain if present, else use a simple approach:
            // Add a ValidationToken property to TenantDomain model if not present (migration required).
            td.GetType().GetProperty("ValidationToken")?.SetValue(td, challenge);

            _db.TenantDomains.Add(td);
            await _db.SaveChangesAsync();
            await PublishAuditAsync("tenant.domain.created", tenantId, new Dictionary<string, string>
            {
                ["domain"] = td.Domain,
                ["validationMethod"] = td.ValidationMethod
            });

            return new TenantDomainDto
            {
                Id = td.Id,
                TenantId = td.TenantId,
                Domain = td.Domain,
                IsValidated = td.IsValidated,
                ValidationMethod = td.ValidationMethod,
                Challenge = challenge,
                CreatedAt = td.CreatedAt
            };
        }

        public async Task<TenantDomainDto> GetDomainAsync(Guid domainId)
        {
            var d = await _db.TenantDomains.FindAsync(domainId);
            if (d == null) return null;
            var challenge = d.GetType().GetProperty("ValidationToken")?.GetValue(d)?.ToString();
            return new TenantDomainDto
            {
                Id = d.Id,
                TenantId = d.TenantId,
                Domain = d.Domain,
                IsValidated = d.IsValidated,
                ValidationMethod = d.ValidationMethod,
                Challenge = challenge,
                CreatedAt = d.CreatedAt
            };
        }

        public async Task<IEnumerable<TenantDomainDto>> ListDomainsAsync(Guid tenantId)
        {
            var list = await _db.TenantDomains.Where(d => d.TenantId == tenantId).ToListAsync();
            return list.Select(d => new TenantDomainDto
            {
                Id = d.Id,
                TenantId = d.TenantId,
                Domain = d.Domain,
                IsValidated = d.IsValidated,
                ValidationMethod = d.ValidationMethod,
                Challenge = d.GetType().GetProperty("ValidationToken")?.GetValue(d)?.ToString(),
                CreatedAt = d.CreatedAt
            });
        }

        public async Task<string> GenerateDnsChallengeAsync(Guid domainId)
        {
            var d = await _db.TenantDomains.FindAsync(domainId);
            if (d == null) throw new KeyNotFoundException();

            var token = GenerateChallengeToken();
            d.GetType().GetProperty("ValidationToken")?.SetValue(d, token);
            d.ValidationMethod = "dns";
            d.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await PublishAuditAsync("tenant.domain.challenge.generated", d.TenantId, new Dictionary<string, string>
            {
                ["domainId"] = d.Id.ToString(),
                ["domain"] = d.Domain,
                ["method"] = "dns"
            });
            return token;
        }

        public async Task<string> GenerateHttpChallengeAsync(Guid domainId)
        {
            var d = await _db.TenantDomains.FindAsync(domainId);
            if (d == null) throw new KeyNotFoundException();

            var token = GenerateChallengeToken();
            d.GetType().GetProperty("ValidationToken")?.SetValue(d, token);
            d.ValidationMethod = "http";
            d.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await PublishAuditAsync("tenant.domain.challenge.generated", d.TenantId, new Dictionary<string, string>
            {
                ["domainId"] = d.Id.ToString(),
                ["domain"] = d.Domain,
                ["method"] = "http"
            });
            return token;
        }

        public async Task<bool> ValidateDomainAsync(Guid domainId)
        {
            var d = await _db.TenantDomains.FindAsync(domainId);
            if (d == null) return false;

            var now = DateTimeOffset.UtcNow;
            if (ValidationAttempts.TryGetValue(domainId, out var lastAttempt) && now - lastAttempt < _validationCooldown)
            {
                var retryAfter = _validationCooldown - (now - lastAttempt);
                _logger.LogInformation("Domain validation rate-limited for {domain} retryAfter={RetryAfter}", d.Domain, retryAfter);
                await PublishAuditAsync("tenant.domain.validation.rate_limited", d.TenantId, new Dictionary<string, string>
                {
                    ["domainId"] = d.Id.ToString(),
                    ["domain"] = d.Domain,
                    ["method"] = d.ValidationMethod,
                    ["retryAfterSeconds"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString()
                });
                throw new DomainValidationRateLimitException(retryAfter);
            }

            ValidationAttempts[domainId] = now;

            var token = d.GetType().GetProperty("ValidationToken")?.GetValue(d)?.ToString();
            if (string.IsNullOrEmpty(token)) return false;

            var domain = d.Domain;

            if (d.ValidationMethod == "dns")
            {
                try
                {
                    var result = await _dnsClient.QueryAsync($"_acme-challenge.{domain}", QueryType.TXT);
                    var txt = result.Answers.TxtRecords().SelectMany(r => r.Text).ToList();
                    if (txt.Contains(token))
                    {
                        d.IsValidated = true;
                        d.GetType().GetProperty("ValidationToken")?.SetValue(d, string.Empty);
                        d.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        ValidationAttempts.TryRemove(domainId, out _);
                        await PublishAuditAsync("tenant.domain.validation.succeeded", d.TenantId, new Dictionary<string, string>
                        {
                            ["domainId"] = d.Id.ToString(),
                            ["domain"] = domain,
                            ["method"] = "dns"
                        });
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DNS validation failed for {domain}", domain);
                    await PublishAuditAsync("tenant.domain.validation.failed", d.TenantId, new Dictionary<string, string>
                    {
                        ["domainId"] = d.Id.ToString(),
                        ["domain"] = domain,
                        ["method"] = "dns",
                        ["reason"] = "exception"
                    });
                    return false;
                }
            }
            else // http
            {
                try
                {
                    // Expect token at http://{domain}/.well-known/acme-challenge/{token}
                    var url = $"http://{domain}/.well-known/acme-challenge/{token}";
                    var resp = await _http.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        if (body.Trim() == token)
                        {
                            d.IsValidated = true;
                            d.GetType().GetProperty("ValidationToken")?.SetValue(d, string.Empty);
                            d.UpdatedAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync();
                            ValidationAttempts.TryRemove(domainId, out _);
                            await PublishAuditAsync("tenant.domain.validation.succeeded", d.TenantId, new Dictionary<string, string>
                            {
                                ["domainId"] = d.Id.ToString(),
                                ["domain"] = domain,
                                ["method"] = "http"
                            });
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "HTTP validation failed for {domain}", domain);
                    await PublishAuditAsync("tenant.domain.validation.failed", d.TenantId, new Dictionary<string, string>
                    {
                        ["domainId"] = d.Id.ToString(),
                        ["domain"] = domain,
                        ["method"] = "http",
                        ["reason"] = "exception"
                    });
                    return false;
                }
            }

            await PublishAuditAsync("tenant.domain.validation.failed", d.TenantId, new Dictionary<string, string>
            {
                ["domainId"] = d.Id.ToString(),
                ["domain"] = domain,
                ["method"] = d.ValidationMethod,
                ["reason"] = "challenge_mismatch"
            });

            return false;
        }

        public async Task<Guid?> ResolveTenantByHostAsync(string host)
        {
            // Try exact domain match
            var domainNormalized = host.Trim().ToLowerInvariant();
            var domain = await _db.TenantDomains.AsNoTracking().FirstOrDefaultAsync(d => d.Domain == domainNormalized && d.IsValidated);
            if (domain != null) return domain.TenantId;

            // Try subdomain resolution: find tenant with PrimaryDomain matching suffix
            var parts = domainNormalized.Split('.');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var candidate = string.Join('.', parts.Skip(i));
                var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(tn => tn.PrimaryDomain == candidate && tn.IsActive);
                if (t != null) return t.Id;
            }

            return null;
        }

        private static string GenerateChallengeToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private async Task PublishAuditAsync(string eventType, Guid tenantId, IDictionary<string, string> data)
        {
            try
            {
                await _auditService.PublishAsync(eventType, tenantId, null, data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit publish failed for {EventType}", eventType);
            }
        }
    }
}
