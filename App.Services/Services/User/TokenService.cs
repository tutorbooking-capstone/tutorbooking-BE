using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using App.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using App.Core.Jsetting;
using App.Repositories.Models.User;  

namespace App.Services.Services.User
{
    public class TokenService : ITokenService
    {
        #region DI Constructor
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public TokenService(
            JwtSettings jwtSettings,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager)
        {
            _jwtSettings = jwtSettings;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        #endregion

        public async Task<TokenResponse> GenerateTokens(AppUser user, string _) 
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            // Add all roles
            foreach (var userRole in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var keyString = _jwtSettings.SecretKey ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // --- Access Token ---
            var accessToken = new JwtSecurityToken(
                claims: claims,
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            // --- Opaque Refresh Token (Recommended approach) ---
            var refreshTokenString = GenerateSecureRefreshToken(); // Generate random string
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "RefreshToken");
            // Store the opaque token (or its hash)
            await _userManager.SetAuthenticationTokenAsync(user, "Default", "RefreshToken", refreshTokenString);

            // --- Response ---
            return new TokenResponse
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshTokenString,
                User = new UserResponse
                {
                    Id = user.Id.ToString(),
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    ProfileImageUrl = user.ProfilePictureUrl ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "unknown",
                    CreatedTime = user.CreatedTime
                }
            };
        }

        // Helper for generating opaque refresh token
        private string GenerateSecureRefreshToken(int size = 64)
        {
            var randomNumber = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }


        public async Task<AppUser?> FindUserByRefreshTokenAsync(string provider, string tokenName, string tokenValue)
        {
            var tokenRepository = _unitOfWork.GetRepository<IdentityUserToken<string>>();
            if (tokenRepository == null)
                throw new InvalidOperationException("Repository for IdentityUserToken<string> not registered.");


            var userToken = await tokenRepository.Entities
                .Where(t => t.LoginProvider == provider && t.Name == tokenName && t.Value == tokenValue)
                .FirstOrDefaultAsync();

            if (userToken == null)
                return null;

             // Optional: Add expiry check for refresh token if storing expiry date separately

            var userRepository = _unitOfWork.GetRepository<AppUser>();
            if (userRepository == null)
                throw new InvalidOperationException("Repository for AppUser not registered.");

            return await userRepository.Entities
                .Where(u => u.Id == userToken.UserId && !u.DeletedTime.HasValue)
                .FirstOrDefaultAsync();
        }
    }
}
