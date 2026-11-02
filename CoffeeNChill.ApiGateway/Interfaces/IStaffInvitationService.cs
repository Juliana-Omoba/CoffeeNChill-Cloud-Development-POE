
using CoffeeNChill.ApiGateway.DTOs;

namespace CoffeeNChill.ApiGateway.Interfaces
{
    // =====================================================
    // POE PART 3: STAFF INVITATION SERVICE CONTRACT
    // =====================================================
    //
    // Defines operations for:
    //
    // - Creating Staff invitations
    // - Retrieving invitations
    // - Revoking invitations
    // - Accepting invitations
    //
    // Implementation belongs in StaffInvitationService.
    // =====================================================

    public interface IStaffInvitationService
    {
        // Create an invitation and return its
        // one-time development testing token.
        Task<(
            StaffInvitationResponseDto Invitation,
            string Token)> CreateAsync(
                CreateStaffInvitationRequestDto request,
                string invitedBy);

        // Retrieve invitation records for Admin review.
        Task<List<StaffInvitationResponseDto>>
            GetAllAsync();

        // Revoke an invitation that is still pending.
        Task<bool> RevokeAsync(
            string invitationId);

        // Accept an invitation and register Staff.
        Task<bool> AcceptAsync(
            AcceptStaffInvitationRequestDto request);
    }
}
