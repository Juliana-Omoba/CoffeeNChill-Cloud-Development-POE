
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: CREATE STAFF INVITATION REQUEST
    // =====================================================
    //
    // PURPOSE:
    //
    // Defines the information an Admin must provide
    // when inviting a new Staff member.
    //
    // SECURITY:
    //
    // The Admin cannot choose the user's role.
    // The system assigns the Staff role automatically.
    //
    // The invited Staff member will create their own
    // password when accepting the invitation.
    // =====================================================

    public class CreateStaffInvitationRequestDto
    {
        // Full name of the Staff member.
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        // Email address where the invitation
        // will eventually be delivered.
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
