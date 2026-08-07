using System;
using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.Services
{
    public sealed class IdentityMetricsSnapshot
    {
        public long LoginSuccessTotal { get; set; }
        public long LoginFailureTotal { get; set; }
        public long LoginAttemptTotal { get; set; }
        public long RefreshFailureTotal { get; set; }
        public long MfaFailureTotal { get; set; }
        public long TokenReplayAttemptTotal { get; set; }
        public long KeyRotationFailureTotal { get; set; }
        public long AuditPublishFailureTotal { get; set; }
        public long CrudActionTotal { get; set; }
        public long CrudCreateTotal { get; set; }
        public long CrudReadTotal { get; set; }
        public long CrudUpdateTotal { get; set; }
        public long CrudDeleteTotal { get; set; }
        public IReadOnlyDictionary<string, long> CrudActionsByKey { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> LoginFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> RefreshFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> MfaFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> KeyRotationFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> AuditPublishFailuresByReason { get; set; } = new Dictionary<string, long>();
    }

    public interface IIdentityTelemetry
    {
        void RecordLoginSuccess(bool mfaEnabled);
        void RecordLoginFailure(string reason);
        void RecordRefreshFailure(string reason);
        void RecordMfaFailure(string reason);
        void RecordTokenReplayAttempt();
        void RecordKeyRotationFailure(string reason);
        void RecordAuditPublishFailure(string reason);
        void RecordCrudAction(string action, string route, string outcome);
        IdentityMetricsSnapshot GetSnapshot();
    }
}
