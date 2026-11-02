
using System.Security.Claims;
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: ADMIN STAFF INVITATION CONTROLLER
    // =====================================================
    //
    // All endpoints require a valid Admin JWT.
    //
    // Customer and Staff JWTs cannot access them.
    // =====================================================

    [ApiController]
    [Route("api/admin/staff/invitations")]
    [Authorize(Roles = "Admin")]
    public class AdminStaffInvitationsController
        : ControllerBase
    {
        private readonly IStaffInvitationService _service;
        private readonly IWebHostEnvironment _environment;

        public AdminStaffInvitationsController(
            IStaffInvitationService service,
            IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        // =================================================
        // POST /api/admin/staff/invitations
        // =================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateStaffInvitationRequestDto request)
        {
            string invitedBy =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.Identity?.Name
                ?? "Unknown";

            try
            {
                var result =
                    await _service.CreateAsync(
                        request,
                        invitedBy);

                // Never expose invitation tokens in
                // production API responses.
                if (_environment.IsDevelopment())
                {
                    return StatusCode(
                        StatusCodes.Status201Created,
                        new
                        {
                            invitation = result.Invitation,
                            developmentInvitationToken =
                                result.Token
                        });
                }

                return StatusCode(
                    StatusCodes.Status201Created,
                    result.Invitation);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        // =================================================
        // GET /api/admin/staff/invitations
        // =================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invitations =
                await _service.GetAllAsync();

            return Ok(invitations);
        }

        // =================================================
        // POST /api/admin/staff/invitations/{id}/revoke
        // =================================================

        [HttpPost("{invitationId}/revoke")]
        public async Task<IActionResult> Revoke(
            string invitationId)
        {
            bool revoked =
                await _service.RevokeAsync(invitationId);

            if (!revoked)
            {
                return Conflict(new
                {
                    message =
                        "Invitation not found or no longer pending."
                });
            }

            return Ok(new
            {
                message = "Invitation revoked successfully."
            });
        }
    }
}
