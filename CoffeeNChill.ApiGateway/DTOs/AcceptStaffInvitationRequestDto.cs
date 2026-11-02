
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: ACCEPT STAFF INVITATION REQUEST
    // =====================================================
    //
    // The Staff member supplies:
    //
    // 1. Invitation ID
    // 2. Invitation token
    // 3. Password for the new Staff account
    //
    // The role and email address are NOT supplied here.
    // They are retrieved from the trusted invitation.
    // =====================================================

    public class AcceptStaffInvitationRequestDto
    {
        [Required]
        public string InvitationId { get; set; }
            = string.Empty;

        [Required]
        public string Token { get; set; }
            = string.Empty;

        [Required]
        [MinLength(12)]
        public string Password { get; set; }
            = string.Empty;
    }
}
