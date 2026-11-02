
using Azure;
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: ADMIN STAFF ACCOUNT MANAGEMENT
    // =====================================================
    //
    // All endpoints require an authenticated Admin.
    //
    // Endpoints:
    //
    // GET   /api/admin/staff
    //
    // PATCH /api/admin/staff/{email}/status
    //
    // Staff accounts cannot access these endpoints.
    // Customers cannot access these endpoints.
    // =====================================================

    [ApiController]
    [Route("api/admin/staff")]
    [Authorize(Roles = "Admin")]
    public class StaffManagementController : ControllerBase
    {
        private readonly IUserTableService _users;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public StaffManagementController(
            IUserTableService users)
        {
            _users = users;
        }

        // =================================================
        // GET ALL STAFF ACCOUNTS
        // =================================================
        //
        // Returns safe account information only.
        //
        // Password hashes are never included.
        // =================================================

        [HttpGet]
        public async Task<ActionResult<
            List<StaffAccountResponseDto>>> GetAllStaff()
        {
            List<UserEntity> staffAccounts =
                await _users.GetAllStaffAsync();

            List<StaffAccountResponseDto> response =
                staffAccounts
                    .Select(MapToResponse)
                    .ToList();

            return Ok(response);
        }

        // =================================================
        // ENABLE OR DISABLE STAFF ACCOUNT
        // =================================================
        //
        // Example:
        //
        // PATCH /api/admin/staff/user@example.com/status
        //
        // Body:
        //
        // {
        //   "isActive": false
        // }
        //
        // Returns:
        // - 200: Updated successfully
        // - 400: Invalid request
        // - 404: Staff account not found
        // - 409: Concurrent update conflict
        // - 401/403: Authentication/role failure
        // =================================================

        [HttpPatch("{email}/status")]
        public async Task<ActionResult<
            StaffAccountResponseDto>> UpdateStatus(
                string email,
                [FromBody] UpdateStaffStatusRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(
                    new
                    {
                        message = "Staff email is required."
                    });
            }

            if (request == null ||
                request.IsActive == null)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The isActive field is required."
                    });
            }

            bool updated;

            try
            {
                updated =
                    await _users.UpdateStaffStatusAsync(
                        email,
                        request.IsActive.Value);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409 ||
                      ex.Status == 412)
            {
                return Conflict(
                    new
                    {
                        message =
                            "The Staff account was modified " +
                            "by another operation. " +
                            "Please retry."
                    });
            }

            if (!updated)
            {
                return NotFound(
                    new
                    {
                        message = "Staff account not found."
                    });
            }

            // Retrieve the latest account details.
            UserEntity? staff =
                await _users.GetUserAsync(
                    "Staff",
                    email);

            if (staff == null)
            {
                return NotFound(
                    new
                    {
                        message = "Staff account not found."
                    });
            }

            return Ok(MapToResponse(staff));
        }

        // =================================================
        // MAP USER ENTITY TO SAFE RESPONSE DTO
        // =================================================

        private static StaffAccountResponseDto MapToResponse(
            UserEntity staff)
        {
            return new StaffAccountResponseDto
            {
                FullName = staff.FullName,

                Email = staff.RowKey,

                Role = staff.PartitionKey,

                // Older records with null are active.
                IsActive = staff.IsActive != false,

                CreatedDate = staff.CreatedDate
            };
        }
    }
}
