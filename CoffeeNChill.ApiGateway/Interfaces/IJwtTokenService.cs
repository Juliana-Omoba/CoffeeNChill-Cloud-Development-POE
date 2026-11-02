
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Interfaces
{
    // =====================================================
    // POE PART 3: JWT TOKEN SERVICE CONTRACT
    // =====================================================
    //
    // Defines how the API Gateway generates a JWT
    // after a user's credentials have been verified.
    //
    // The implementation will include identity claims,
    // a role claim, an expiry and a digital signature.
    // =====================================================

    public interface IJwtTokenService
    {
        LoginResponseDto GenerateToken(UserEntity user);
    }
}
