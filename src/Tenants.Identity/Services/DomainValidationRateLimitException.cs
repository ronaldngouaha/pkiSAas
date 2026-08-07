using System;

namespace Acme.Pki.Tenants.Identity.Services
{
    public sealed class DomainValidationRateLimitException : Exception
    {
        public DomainValidationRateLimitException(TimeSpan retryAfter)
            : base("Domain validation is temporarily rate limited.")
        {
            RetryAfter = retryAfter;
        }

        public TimeSpan RetryAfter { get; }
    }
}