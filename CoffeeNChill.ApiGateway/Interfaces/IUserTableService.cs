
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Interfaces
{
    // =====================================================
    // POE PART 3: USER STORAGE INTERFACE
    // =====================================================
    //
    // Preserves:
    // - Customer registration
    // - Invited Staff registration
    // - Login account retrieval
    // - Role-based account retrieval
    //
    // Adds:
    // - Staff account listing
    // - Staff account activation/deactivation
    // =====================================================

    public interface IUserTableService
    {
        Task<TableClient> GetTableClientAsync();

        Task<UserEntity?> GetUserAsync(
            string role,
            string email);

        Task<UserEntity?> GetUserByEmailAsync(
            string email);

        Task<bool> RegisterCustomerAsync(
            UserEntity user);

        Task<bool> RegisterInvitedStaffAsync(
            UserEntity user);

        // Retrieve all Staff accounts.
        Task<List<UserEntity>> GetAllStaffAsync();

        // Enable or disable an existing Staff account.
        // Returns false when no Staff account exists.
        Task<bool> UpdateStaffStatusAsync(
            string email,
            bool isActive);
    }
}
