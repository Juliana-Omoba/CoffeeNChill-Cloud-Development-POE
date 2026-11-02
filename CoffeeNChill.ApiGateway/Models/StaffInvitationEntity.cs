
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models
{
    // ============================================================
    // POE PART 3: STAFF INVITATION ENTITY
    // ============================================================
    //
    // PURPOSE:
    // Stores Staff invitations in Azure Table Storage.
    //
    // IMPORTANT:
    // An invitation is NOT a Staff account.
    //
    // Staff accounts are created only after a valid invitation
    // has been accepted and a password has been provided.
    //
    // Azure Table:
    // StaffInvitations
    //
    // PartitionKey = "Invitation"
    // RowKey       = unique invitation identifier
    // ============================================================

    public class StaffInvitationEntity : ITableEntity
    {
        // --------------------------------------------------------
        // Azure Table Storage partition.
        // --------------------------------------------------------
        public string PartitionKey { get; set; } = "Invitation";

        // --------------------------------------------------------
        // Unique invitation identifier.
        //
        // A GUID will be generated when an Admin creates
        // an invitation.
        // --------------------------------------------------------
        public string RowKey { get; set; } = string.Empty;

        // --------------------------------------------------------
        // Email address of the invited Staff member.
        // --------------------------------------------------------
        public string Email { get; set; } = string.Empty;

        // --------------------------------------------------------
        // Full name of the invited Staff member.
        // --------------------------------------------------------
        public string FullName { get; set; } = string.Empty;

        // --------------------------------------------------------
        // Store a hash of the invitation token.
        //
        // Never store the original invitation token.
        // --------------------------------------------------------
        public string TokenHash { get; set; } = string.Empty;

        // --------------------------------------------------------
        // Date and time the invitation was created.
        // --------------------------------------------------------
        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        // --------------------------------------------------------
        // Date and time the invitation expires.
        // --------------------------------------------------------
        public DateTimeOffset ExpiresAt { get; set; }

        // --------------------------------------------------------
        // Invitation status:
        //
        // Pending
        // Accepted
        // Revoked
        //
        // Expiration is determined using ExpiresAt.
        // --------------------------------------------------------
        public string Status { get; set; } = "Pending";

        // --------------------------------------------------------
        // Email address of the Admin who issued the invitation.
        // --------------------------------------------------------
        public string InvitedBy { get; set; } = string.Empty;

        // --------------------------------------------------------
        // Records when the invitation was accepted.
        // --------------------------------------------------------
        public DateTimeOffset? AcceptedAt { get; set; }

        // --------------------------------------------------------
        // Required Azure Table Storage properties.
        // --------------------------------------------------------
        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
