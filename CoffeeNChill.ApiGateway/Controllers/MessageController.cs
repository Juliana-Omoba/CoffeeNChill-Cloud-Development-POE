
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: JWT AUTHENTICATION AND ROLE TESTING
    // =====================================================
    //
    // PURPOSE:
    // Demonstrate authentication and role-based access
    // control through the API Gateway.
    //
    // The controller provides three endpoints:
    //
    // 1. POST /api/Message
    //    Requires a valid JWT access token.
    //
    // 2. GET /api/Message/customer
    //    Requires the Customer role.
    //
    // 3. GET /api/Message/staff
    //    Requires the Staff or Admin role.
    //
    // These endpoints are used to verify that JWT
    // authentication and role-based authorisation
    // work correctly.
    // =====================================================

    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        // =================================================
        // TEST 4A: AUTHENTICATED MESSAGE ENDPOINT
        // =================================================
        //
        // [Authorize] means that only users with a valid
        // JWT access token may call this endpoint.
        //
        // An unauthenticated request should return:
        // 401 Unauthorized.
        //
        // An authenticated request should return:
        // 200 OK.
        // =================================================

        [HttpPost]
        [Authorize]
        public IActionResult Send([FromBody] MessageDto dto)
        {
            // Preserve the original Message functionality.

            return Ok(new
            {
                received = dto.Text
            });
        }

        // =================================================
        // TEST 4B: CUSTOMER ROLE AUTHORISATION
        // =================================================
        //
        // Only authenticated users with the Customer
        // role may access this endpoint.
        //
        // Expected results:
        //
        // Customer JWT -> 200 OK
        // Staff JWT    -> 403 Forbidden
        // No JWT       -> 401 Unauthorized
        // =================================================

        [HttpGet("customer")]
        [Authorize(Roles = "Customer")]
        public IActionResult CustomerOnly()
        {
            return Ok(new
            {
                message = "Customer access granted.",
                role = "Customer"
            });
        }

        // =================================================
        // TEST 4C: STAFF OR ADMIN ROLE AUTHORISATION
        // =================================================
        //
        // Only authenticated Staff or Admin users
        // may access this endpoint.
        //
        // Expected results:
        //
        // Customer JWT -> 403 Forbidden
        // Staff JWT    -> 200 OK
        // Admin JWT    -> 200 OK
        // No JWT       -> 401 Unauthorized
        // =================================================

        [HttpGet("staff")]
        [Authorize(Roles = "Staff,Admin")]
        public IActionResult StaffOnly()
        {
            return Ok(new
            {
                message = "Staff or Admin access granted."
            });
        }
    }

    // =====================================================
    // EXISTING MESSAGE DATA TRANSFER OBJECT
    // =====================================================
    //
    // This record preserves the original request format
    // used by POST /api/Message.
    // =====================================================

    public record MessageDto(string Text);
}
