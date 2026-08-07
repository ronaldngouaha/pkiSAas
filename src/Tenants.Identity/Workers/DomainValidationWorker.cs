using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Workers
{
    public class DomainValidationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DomainValidationWorker> _logger;
        private readonly TimeSpan _interval;

        public DomainValidationWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DomainValidationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var intervalMinutes = configuration.GetValue<int?>("DomainValidation:IntervalMinutes") ?? 5;
            _interval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DomainValidationWorker started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TenantsIdentityDbContext>();
                    var domainService = scope.ServiceProvider.GetRequiredService<IDomainService>();

                    var pending = await db.TenantDomains
                        .Where(d => !d.IsValidated && !string.IsNullOrEmpty(d.ValidationToken))
                        .ToListAsync(stoppingToken);

                    foreach (var d in pending)
                    {
                        try
                        {
                            var ok = await domainService.ValidateDomainAsync(d.Id);
                            if (ok)
                            {
                                _logger.LogInformation("Domain {domain} validated.", d.Domain);
                            }
                            else
                            {
                                _logger.LogInformation("Domain {domain} still pending validation.", d.Domain);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Validation attempt failed for {domain}", d.Domain);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DomainValidationWorker loop error");
                }

                await Task.Delay(_interval, stoppingToken);
            }
            _logger.LogInformation("DomainValidationWorker stopping.");
        }
    }
}
