
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: AUTHENTICATION CONTROLLER
    // =====================================================
    //
    // ENDPOINTS:
    //
    // POST /api/auth/register
    // POST /api/auth/login
    // POST /api/auth/accept-invitation
    //
    // Public registration creates Customer accounts.
    //
    // Invitation acceptance creates Staff accounts
    // only after successful token validation.
    // =====================================================

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly IStaffInvitationService
            _invitationService;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public AuthController(
            IAuthService authService,
            IStaffInvitationService invitationService)
        {
            _authService = authService;
            _invitationService = invitationService;
        }

        // =================================================
        // POST /api/auth/register
        // =================================================
        //
        // Public Customer registration.
        // =================================================

        /// <summary>
        /// Registers a new CoffeeNChill Customer.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequestDto request)
        {
            bool created =
                await _authService.RegisterAsync(
                    request);

            if (!created)
            {
                return Conflict(new
                {
                    message =
                        "This email is already registered."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message =
                        "Customer registered successfully."
                });
        }

        // =================================================
        // POST /api/auth/login
        // =================================================
        //
        // Authenticates Customer, Staff and Admin users.
        // =================================================

        /// <summary>
        /// Authenticates a user and returns a signed JWT.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(LoginResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>>
            Login(
                [FromBody] LoginRequestDto request)
        {
            var result =
                await _authService.LoginAsync(
                    request);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message =
                        "Invalid email or password."
                });
            }

            return Ok(result);
        }

        // =================================================
        // POST /api/auth/accept-invitation
        // =================================================
        //
        // Allows invited Staff members to set their
        // password and activate their account.
        //
        // Anonymous access is necessary because
        // the invited person has no JWT yet.
        // =================================================

        /// <summary>
        /// Accepts a Staff invitation and creates the account.
        /// </summary>
        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptInvitation(
            [FromBody] AcceptStaffInvitationRequestDto request)
        {
            bool accepted =
                await _invitationService.AcceptAsync(
                    request);

            if (!accepted)
            {
                return BadRequest(new
                {
                    message =
                        "The invitation is invalid, expired, " +
                        "already used, or cannot be accepted."
                });
            }

            return Ok(new
            {
                message =
                    "Staff account created successfully. " +
                    "You can now log in."
            });
        }
    }
}
