
namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: STAFF INVITATION RESPONSE
    // =====================================================
    //
    // PURPOSE:
    //
    // Return invitation details to an Admin without
    // exposing the stored invitation token hash.
    //
    // IMPORTANT:
    //
    // The invitation token itself must not be returned
    // in the ordinary invitation listing.
    // =====================================================

    public class StaffInvitationResponseDto
    {
        public string InvitationId { get; set; }
            = string.Empty;

        public string FullName { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public string InvitedBy { get; set; }
            = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? AcceptedAt { get; set; }
    }
}
