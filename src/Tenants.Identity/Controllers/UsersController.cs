using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints de gestion des utilisateurs d'un tenant.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "TenantAdminOrUserManagerPolicy")]
    [Route("api/v1/tenants/{tenantId:guid}/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        /// <summary>
        /// Lien pour creer un utilisateur dans un tenant.
        /// </summary>
        [ProducesResponseType(typeof(UserSingleEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Create(Guid tenantId, [FromBody] UserCreateDto dto)
        {
            var user = await _service.CreateAsync(tenantId, dto);
            return BuildEnvelope(StatusCodes.Status201Created, user, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour recuperer un utilisateur par son identifiant.
        /// </summary>
        [ProducesResponseType(typeof(UserSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> Get(Guid tenantId, Guid userId)
        {
            var user = await _service.GetAsync(tenantId, userId);
            if (user == null)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
            }

            return BuildEnvelope(StatusCodes.Status200OK, user, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour modifier les donnees d'un utilisateur du tenant.
        /// </summary>
        [ProducesResponseType(typeof(UserSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> Update(Guid tenantId, Guid userId, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var user = await _service.UpdateAsync(tenantId, userId, dto);
                if (user == null)
                {
                    return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
                }

                return BuildEnvelope(StatusCodes.Status200OK, user, "Requete traitee avec succes.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien pour lister les utilisateurs d'un tenant avec pagination.
        /// </summary>
        [ProducesResponseType(typeof(UserListEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> List(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var users = await _service.ListAsync(tenantId, page, pageSize);
            return BuildEnvelope(StatusCodes.Status200OK, users, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour desactiver un utilisateur.
        /// </summary>
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("{userId:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid tenantId, Guid userId, [FromBody] ReasonRequest req)
        {
            try
            {
                await _service.DeactivateAsync(tenantId, userId, req.Reason);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
            }
        }

        /// <summary>
        /// Lien pour reactiver un utilisateur.
        /// </summary>
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("{userId:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid tenantId, Guid userId, [FromBody] ReasonRequest req)
        {
            try
            {
                await _service.ReactivateAsync(tenantId, userId, req.Reason);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
            }
        }

        /// <summary>
        /// Lien pour modifier le mot de passe d'un utilisateur du tenant.
        /// </summary>
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{userId:guid}/password")]
        public async Task<IActionResult> ChangePassword(Guid tenantId, Guid userId, [FromBody] ChangePasswordRequestDto req)
        {
            try
            {
                await _service.ChangePasswordAsync(tenantId, userId, req.NewPassword);
                return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien pour ajouter/modifier le role d'un utilisateur du tenant.
        /// </summary>
        [ProducesResponseType(typeof(UserSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{userId:guid}/role")]
        [Authorize(Policy = "TenantAdminSensitivePolicy")]
        public async Task<IActionResult> AddRole(Guid tenantId, Guid userId, [FromBody] AddRoleRequestDto req)
        {
            try
            {
                var user = await _service.AddRoleAsync(tenantId, userId, req.Role);
                return BuildEnvelope(StatusCodes.Status200OK, user, "Requete traitee avec succes.");
            }
            catch (KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Utilisateur introuvable.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
        }

        public class ReasonRequest { public string Reason { get; set; } }

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