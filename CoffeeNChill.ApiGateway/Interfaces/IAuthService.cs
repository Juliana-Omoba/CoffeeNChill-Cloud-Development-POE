
using CoffeeNChill.ApiGateway.DTOs;

namespace CoffeeNChill.ApiGateway.Interfaces
{
    // =====================================================
    // AUTHENTICATION SERVICE CONTRACT
    // =====================================================
    //
    // RegisterAsync: preserves Customer registration.
    //
    // LoginAsync: validates credentials and returns
    // a signed JWT when authentication succeeds.
    // =====================================================

    public interface IAuthService
    {
        Task<bool> RegisterAsync(
            RegisterRequestDto request);

        Task<LoginResponseDto?> LoginAsync(
            LoginRequestDto request);
    }
}
