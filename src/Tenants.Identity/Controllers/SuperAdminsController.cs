using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.SuperAdmin;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints de creation et gestion des comptes SuperAdmin.
    /// </summary>
    [ApiController]
    [Route("api/v1/superadmins")]
    public class SuperAdminsController : ControllerBase
    {
        private readonly ISuperAdminService _service;

        public SuperAdminsController(ISuperAdminService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lien pour creer un compte SuperAdmin.
        /// </summary>
        /// <remarks>
        /// Autorise sans bearer uniquement pour le bootstrap initial, quand aucun SuperAdmin actif n'existe encore en base.
        /// Des qu'un SuperAdmin existe deja, le bearer token devient obligatoire et il doit appartenir a un autre SuperAdmin.
        /// </remarks>
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SuperAdminCreateDto dto)
        {
            try
            {
                var hasAnyActive = await _service.AnyActiveSuperAdminAsync();
                if (hasAnyActive && !IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: seul un SuperAdmin peut creer un autre SuperAdmin.");
                }

                var created = await _service.CreateAsync(dto);
                return BuildEnvelope(StatusCodes.Status201Created, created, "Requete traitee avec succes.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status409Conflict, null, ex.Message);
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour recuperer un SuperAdmin par identifiant.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var user = await _service.GetAsync(id);
                if (user == null)
                {
                    return BuildEnvelope(StatusCodes.Status404NotFound, null, "SuperAdmin introuvable.");
                }

                return BuildEnvelope(StatusCodes.Status200OK, user, "Requete traitee avec succes.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour modifier les donnees d'un SuperAdmin.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SuperAdminUpdateDto dto)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var user = await _service.UpdateAsync(id, dto);
                if (user == null)
                {
                    return BuildEnvelope(StatusCodes.Status404NotFound, null, "SuperAdmin introuvable.");
                }

                return BuildEnvelope(StatusCodes.Status200OK, user, "Requete traitee avec succes.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour lister les SuperAdmins.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var users = await _service.ListAsync(page, pageSize, includeInactive);
                return BuildEnvelope(StatusCodes.Status200OK, users, "Requete traitee avec succes.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour desactiver un SuperAdmin.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] SuperAdminStatusRequestDto request)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                await _service.DeactivateAsync(id, request.Reason);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "SuperAdmin introuvable.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status409Conflict, null, ex.Message);
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour reactiver un SuperAdmin.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("{id:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id, [FromBody] SuperAdminStatusRequestDto request)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                await _service.ReactivateAsync(id, request.Reason);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "SuperAdmin introuvable.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour modifier le mot de passe d'un SuperAdmin.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id:guid}/password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                await _service.ChangePasswordAsync(id, request.NewPassword);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "SuperAdmin introuvable.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour creer un TenantAdmin dans un tenant cible.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminCreateEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("tenants/{tenantId:guid}/users/tenant-admin")]
        public async Task<IActionResult> CreateTenantAdmin(Guid tenantId, [FromBody] TenantAdminCreateDto dto)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var created = await _service.CreateTenantAdminAsync(tenantId, dto);
                return BuildEnvelope(StatusCodes.Status201Created, created, "Requete traitee avec succes.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status409Conflict, null, ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Tenant introuvable.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour lister les utilisateurs d'un tenant.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminListEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet("tenants/{tenantId:guid}/users")]
        public async Task<IActionResult> GetTenantUsers(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var users = await _service.ListTenantUsersAsync(tenantId, page, pageSize);
                return BuildEnvelope(StatusCodes.Status200OK, users, "Requete traitee avec succes.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        /// <summary>
        /// Lien pour reinitialiser le mot de passe d'un utilisateur tenant vers la valeur par defaut.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(SuperAdminEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPatch("tenants/{tenantId:guid}/users/{userId:guid}/reset-default-password")]
        public async Task<IActionResult> ResetTenantUserPasswordToDefault(Guid tenantId, Guid userId)
        {
            try
            {
                if (!IsCurrentUserSuperAdmin())
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
                }

                var defaultPassword = await _service.ResetTenantUserPasswordToDefaultAsync(tenantId, userId);
                return BuildEnvelope(StatusCodes.Status200OK, new { tenantId, userId, defaultPassword }, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable pour ce tenant.");
            }
            catch (Exception ex)
            {
                return BuildEnvelope(StatusCodes.Status500InternalServerError, null, $"Erreur serveur: {ex.Message}");
            }
        }

        private bool IsCurrentUserSuperAdmin()
        {
            return User.Identity?.IsAuthenticated == true && User.HasClaim("roles", "SuperAdmin");
        }

        private IActionResult BuildEnvelope(int statusCode, object? data, string message)
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
