
using System;

namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: JWT LOGIN RESPONSE DTO
    // =====================================================
    //
    // PURPOSE:
    // Defines the information returned after a user
    // successfully logs in.
    //
    // SECURITY:
    // The response contains an access token.
    // It must never contain the user's password
    // or stored password hash.
    //
    // =====================================================

    public class LoginResponseDto
    {
        // Signed JWT used to access protected endpoints.
        public string AccessToken { get; set; }
            = string.Empty;

        // Indicates how the token must be supplied.
        public string TokenType { get; set; }
            = "Bearer";

        // UTC date and time when the JWT expires.
        public DateTime ExpiresAtUtc { get; set; }

        // Role assigned to the authenticated user.
        public string Role { get; set; }
            = string.Empty;

        // Display name of the authenticated user.
        public string FullName { get; set; }
            = string.Empty;
    }
}
