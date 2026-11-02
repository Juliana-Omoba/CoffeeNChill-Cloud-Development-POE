
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: JWT TOKEN GENERATION
    // =====================================================
    //
    // Creates signed JWT access tokens for users who
    // have successfully authenticated.
    //
    // Claims:
    // - Name
    // - Email
    // - Role
    //
    // The token is signed using HMAC SHA-256.
    // =====================================================

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginResponseDto GenerateToken(UserEntity user)
        {
            // Read JWT settings from configuration.
            string issuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "Jwt:Issuer is missing.");

            string audience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "Jwt:Audience is missing.");

            string secret =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is missing.");

            // Require a sufficiently long secret.
            // Production secrets must be randomly generated
            // and stored outside source control.
            if (Encoding.UTF8.GetByteCount(secret) < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key must be at least 32 bytes.");
            }

            // Read the token lifetime in minutes.
            if (!int.TryParse(
                    _configuration["Jwt:ExpiryMinutes"],
                    out int expiryMinutes)
                || expiryMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:ExpiryMinutes must be positive.");
            }

            DateTime expiresAtUtc =
                DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Create the authenticated user's claims.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.RowKey),

                new Claim(
                    ClaimTypes.Role,
                    user.PartitionKey),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            // Create the symmetric signing key.
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret));

            // Configure the signing algorithm.
            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            // Build the JWT.
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            // Serialize the JWT for the API response.
            string accessToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = expiresAtUtc,
                Role = user.PartitionKey,
                FullName = user.FullName
            };
        }
    }
}
