
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    // =====================================================
    // POE PART 3: UPDATE STAFF STATUS REQUEST
    // =====================================================
    //
    // true  = Enable Staff account
    // false = Disable Staff account
    //
    // Nullable bool with Required ensures that
    // the API rejects a missing isActive property.
    // =====================================================

    public class UpdateStaffStatusRequestDto
    {
        [Required]
        public bool? IsActive { get; set; }
    }
}
