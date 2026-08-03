using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/secure")]
    public class SecureExampleController : ControllerBase
    {
        [HttpPost("tenant-owner/suspend/{tenantId:guid}")]
        [Authorize(Policy = "TenantOwnerPolicy")]
        public IActionResult TenantOwnerSuspend(Guid tenantId)
        {
            return Ok(new { message = "TenantOwner action allowed", tenantId });
        }

        [HttpPost("tenant-action/{tenantId:guid}")]
        [Authorize(Policy = "TenantAdminPolicy")]
        public IActionResult TenantAction(Guid tenantId)
        {
            return Ok(new { message = "Tenant admin action allowed", tenantId });
        }

        [HttpPost("tenant-sensitive")]
        [Authorize(Policy = "TenantAdminSensitivePolicy")]
        public IActionResult TenantSensitiveAction()
        {
            return Ok(new { message = "Tenant admin sensitive action allowed with MFA" });
        }

        [HttpPost("security-admin/audit")]
        [Authorize(Policy = "SecurityAdminPolicy")]
        public IActionResult SecurityAdminAction()
        {
            return Ok(new { message = "Security admin action allowed with MFA" });
        }

        [HttpPost("app-admin/credentials")]
        [Authorize(Policy = "AppAdminPolicy")]
        public IActionResult AppAdminAction()
        {
            return Ok(new { message = "App admin action allowed with approval workflow" });
        }

        [HttpPost("user-manager/reset-password/{tenantId:guid}")]
        [Authorize(Policy = "UserManagerPolicy")]
        public IActionResult UserManagerAction(Guid tenantId)
        {
            return Ok(new { message = "User manager action allowed", tenantId });
        }

        [HttpPost("support-agent/unlock/{tenantId:guid}")]
        [Authorize(Policy = "SupportAgentPolicy")]
        public IActionResult SupportAgentAction(Guid tenantId)
        {
            return Ok(new { message = "Support agent action allowed in time-limited session", tenantId });
        }

        [HttpPost("end-user/profile/{tenantId:guid}/{userId:guid}")]
        [Authorize(Policy = "EndUserOwnResourcePolicy")]
        public IActionResult EndUserAction(Guid tenantId, Guid userId)
        {
            return Ok(new { message = "End user own-resource action allowed", tenantId, userId });
        }

        [HttpPost("service-account/task")]
        [Authorize(Policy = "ServiceAccountPolicy")]
        public IActionResult ServiceAccountAction()
        {
            return Ok(new { message = "Service account action allowed with restricted scope" });
        }

        [HttpGet("viewer/metrics/{tenantId:guid}")]
        [Authorize(Policy = "ViewerPolicy")]
        public IActionResult ViewerAction(Guid tenantId)
        {
            return Ok(new { message = "Viewer read-only action allowed", tenantId });
        }

        [HttpGet("readonly-admin/diagnostics/{tenantId:guid}")]
        [Authorize(Policy = "ReadOnlyAdminPolicy")]
        public IActionResult ReadOnlyAdminAction(Guid tenantId)
        {
            return Ok(new { message = "ReadOnlyAdmin diagnostics allowed", tenantId });
        }

        [HttpPost("sensitive")]
        [Authorize(Policy = "RequireMfa")]
        public IActionResult SensitiveAction()
        {
            return Ok(new { message = "MFA verified for this action" });
        }

        [HttpPost("rotate-keys")]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult RotateKeys()
        {
            return Ok(new { message = "Key rotation initiated" });
        }
    }
}