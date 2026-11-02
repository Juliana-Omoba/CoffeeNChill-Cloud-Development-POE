
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Interfaces
{
    // ============================================================
    // POE PART 3: STAFF INVITATION STORAGE CONTRACT
    // ============================================================
    //
    // Defines operations for storing and retrieving
    // Staff invitations in Azure Table Storage.
    //
    // Invitation creation and acceptance will be
    // handled by a separate business service.
    // ============================================================

    public interface IStaffInvitationTableService
    {
        // Save a newly created invitation.
        Task CreateInvitationAsync(
            StaffInvitationEntity invitation);

        // Retrieve an invitation by its unique identifier.
        Task<StaffInvitationEntity?> GetInvitationAsync(
            string invitationId);

        // Retrieve all invitations for Admin review.
        Task<List<StaffInvitationEntity>> GetAllInvitationsAsync();

        // Update an existing invitation using optimistic
        // concurrency to prevent conflicting changes.
        Task UpdateInvitationAsync(
            StaffInvitationEntity invitation);
    }
}
