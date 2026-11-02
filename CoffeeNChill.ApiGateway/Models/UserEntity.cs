
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models
{
    // =====================================================
    // POE PART 3: USER ENTITY
    // =====================================================
    //
    // Azure Table: Users
    //
    // PartitionKey:
    // - Customer
    // - Staff
    // - Admin
    //
    // RowKey:
    // - Normalised email address
    //
    // IsActive:
    // - true: Account enabled
    // - false: Account disabled
    // - null: Legacy account, treated as enabled
    //
    // Nullable bool preserves compatibility with
    // existing Azure Table records that were created
    // before account management was implemented.
    // =====================================================

    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
            = "Customer";

        public string RowKey { get; set; }
            = string.Empty;

        public string FullName { get; set; }
            = string.Empty;

        // Only the password hash is stored.
        public string PasswordHash { get; set; }
            = string.Empty;

        public DateTimeOffset CreatedDate { get; set; }
            = DateTimeOffset.UtcNow;

        // Existing accounts without this field
        // remain enabled for backward compatibility.
        public bool? IsActive { get; set; } = true;

        // Required Azure Table Storage metadata.
        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
