using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

namespace Acme.Pki.Tenants.Identity.Services
{
    public sealed class IdentityTelemetry : IIdentityTelemetry, IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _loginAttempts;
        private readonly Counter<long> _loginFailures;
        private readonly Counter<long> _refreshFailures;
        private readonly Counter<long> _mfaFailures;
        private readonly Counter<long> _tokenReplayAttempts;
        private readonly Counter<long> _keyRotationFailures;
        private readonly Counter<long> _auditPublishFailures;
        private long _loginSuccessTotal;
        private long _loginFailureTotal;
        private long _refreshFailureTotal;
        private long _mfaFailureTotal;
        private long _tokenReplayAttemptTotal;
        private long _keyRotationFailureTotal;
        private long _auditPublishFailureTotal;
        private long _crudActionTotal;
        private long _crudCreateTotal;
        private long _crudReadTotal;
        private long _crudUpdateTotal;
        private long _crudDeleteTotal;
        private readonly ConcurrentDictionary<string, long> _loginFailuresByReason = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _refreshFailuresByReason = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _mfaFailuresByReason = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _keyRotationFailuresByReason = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _auditPublishFailuresByReason = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _crudActionsByKey = new(StringComparer.OrdinalIgnoreCase);

        public IdentityTelemetry()
        {
            _meter = new Meter("Acme.Pki.Tenants.Identity.Auth", "1.0.0");
            _loginAttempts = _meter.CreateCounter<long>("auth_login_attempts_total", description: "Total login attempts.");
            _loginFailures = _meter.CreateCounter<long>("auth_login_failures_total", description: "Total login failures.");
            _refreshFailures = _meter.CreateCounter<long>("auth_refresh_failures_total", description: "Total refresh token failures.");
            _mfaFailures = _meter.CreateCounter<long>("auth_mfa_failures_total", description: "Total MFA validation failures.");
            _tokenReplayAttempts = _meter.CreateCounter<long>("auth_token_replay_attempts_total", description: "Total detected refresh token replay attempts.");
            _keyRotationFailures = _meter.CreateCounter<long>("auth_key_rotation_failures_total", description: "Total key retrieval/rotation failures.");
            _auditPublishFailures = _meter.CreateCounter<long>("auth_audit_publish_failures_total", description: "Total audit publication failures.");
        }

        public void RecordLoginSuccess(bool mfaEnabled)
        {
            _loginAttempts.Add(1, new KeyValuePair<string, object?>("outcome", "success"), new KeyValuePair<string, object?>("mfaEnabled", mfaEnabled));
            Interlocked.Increment(ref _loginSuccessTotal);
        }

        public void RecordLoginFailure(string reason)
        {
            var normalizedReason = Normalize(reason);
            _loginAttempts.Add(1, new KeyValuePair<string, object?>("outcome", "failure"), new KeyValuePair<string, object?>("reason", normalizedReason));
            _loginFailures.Add(1, new KeyValuePair<string, object?>("reason", normalizedReason));
            Interlocked.Increment(ref _loginFailureTotal);
            _loginFailuresByReason.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);
        }

        public void RecordRefreshFailure(string reason)
        {
            var normalizedReason = Normalize(reason);
            _refreshFailures.Add(1, new KeyValuePair<string, object?>("reason", normalizedReason));
            Interlocked.Increment(ref _refreshFailureTotal);
            _refreshFailuresByReason.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);
        }

        public void RecordMfaFailure(string reason)
        {
            var normalizedReason = Normalize(reason);
            _mfaFailures.Add(1, new KeyValuePair<string, object?>("reason", normalizedReason));
            Interlocked.Increment(ref _mfaFailureTotal);
            _mfaFailuresByReason.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);
        }

        public void RecordTokenReplayAttempt()
        {
            _tokenReplayAttempts.Add(1);
            Interlocked.Increment(ref _tokenReplayAttemptTotal);
        }

        public void RecordKeyRotationFailure(string reason)
        {
            var normalizedReason = Normalize(reason);
            _keyRotationFailures.Add(1, new KeyValuePair<string, object?>("reason", normalizedReason));
            Interlocked.Increment(ref _keyRotationFailureTotal);
            _keyRotationFailuresByReason.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);
        }

        public void RecordAuditPublishFailure(string reason)
        {
            var normalizedReason = Normalize(reason);
            _auditPublishFailures.Add(1, new KeyValuePair<string, object?>("reason", normalizedReason));
            Interlocked.Increment(ref _auditPublishFailureTotal);
            _auditPublishFailuresByReason.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);
        }

        public void RecordCrudAction(string action, string route, string outcome)
        {
            var normalizedAction = Normalize(action);
            var normalizedRoute = string.IsNullOrWhiteSpace(route) ? "unknown" : route.Trim().ToLowerInvariant();
            var normalizedOutcome = Normalize(outcome);

            Interlocked.Increment(ref _crudActionTotal);
            switch (normalizedAction)
            {
                case "create":
                    Interlocked.Increment(ref _crudCreateTotal);
                    break;
                case "read":
                    Interlocked.Increment(ref _crudReadTotal);
                    break;
                case "update":
                    Interlocked.Increment(ref _crudUpdateTotal);
                    break;
                case "delete":
                    Interlocked.Increment(ref _crudDeleteTotal);
                    break;
            }

            var key = $"{normalizedAction}|{normalizedRoute}|{normalizedOutcome}";
            _crudActionsByKey.AddOrUpdate(key, 1, (_, current) => current + 1);
        }

        public IdentityMetricsSnapshot GetSnapshot()
        {
            var loginSuccess = Interlocked.Read(ref _loginSuccessTotal);
            var loginFailure = Interlocked.Read(ref _loginFailureTotal);

            return new IdentityMetricsSnapshot
            {
                LoginSuccessTotal = loginSuccess,
                LoginFailureTotal = loginFailure,
                LoginAttemptTotal = loginSuccess + loginFailure,
                RefreshFailureTotal = Interlocked.Read(ref _refreshFailureTotal),
                MfaFailureTotal = Interlocked.Read(ref _mfaFailureTotal),
                TokenReplayAttemptTotal = Interlocked.Read(ref _tokenReplayAttemptTotal),
                KeyRotationFailureTotal = Interlocked.Read(ref _keyRotationFailureTotal),
                AuditPublishFailureTotal = Interlocked.Read(ref _auditPublishFailureTotal),
                CrudActionTotal = Interlocked.Read(ref _crudActionTotal),
                CrudCreateTotal = Interlocked.Read(ref _crudCreateTotal),
                CrudReadTotal = Interlocked.Read(ref _crudReadTotal),
                CrudUpdateTotal = Interlocked.Read(ref _crudUpdateTotal),
                CrudDeleteTotal = Interlocked.Read(ref _crudDeleteTotal),
                CrudActionsByKey = ToOrderedDictionary(_crudActionsByKey),
                LoginFailuresByReason = ToOrderedDictionary(_loginFailuresByReason),
                RefreshFailuresByReason = ToOrderedDictionary(_refreshFailuresByReason),
                MfaFailuresByReason = ToOrderedDictionary(_mfaFailuresByReason),
                KeyRotationFailuresByReason = ToOrderedDictionary(_keyRotationFailuresByReason),
                AuditPublishFailuresByReason = ToOrderedDictionary(_auditPublishFailuresByReason)
            };
        }

        public void Dispose()
        {
            _meter.Dispose();
        }

        private static string Normalize(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim().ToLowerInvariant();
        }

        private static IReadOnlyDictionary<string, long> ToOrderedDictionary(ConcurrentDictionary<string, long> source)
        {
            return source
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
