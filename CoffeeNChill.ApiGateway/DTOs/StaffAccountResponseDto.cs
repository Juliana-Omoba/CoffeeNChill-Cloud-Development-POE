
namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: STAFF ACCOUNT RESPONSE
    // =====================================================
    //
    // Safe representation of a Staff account.
    //
    // Excludes:
    // - PasswordHash
    // - Azure Table ETag
    // - Internal storage metadata
    // =====================================================

    public class StaffAccountResponseDto
    {
        public string FullName { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string Role { get; set; }
            = "Staff";

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
    }
}
