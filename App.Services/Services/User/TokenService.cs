using App.Core.Base;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using App.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace App.Services.Services.User
{
    public class TokenService : ITokenService
    {
        #region DI Constructor
        private readonly JwtSettings _jwtSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public TokenService(
            JwtSettings jwtSettings,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager)
        {
            _jwtSettings = jwtSettings;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        #endregion

        public async Task<TokenResponse> GenerateTokens(AppUser user, string role)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim("id", user.Id.ToString()),
                new Claim("role", role),
            };

            var keyString = _jwtSettings.SecretKey ?? string.Empty;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var claimsIdentity = new ClaimsIdentity(claims, "Bearer");
            if (_httpContextAccessor.HttpContext != null)
            {
                var principal = new ClaimsPrincipal(new[] { claimsIdentity });
                _httpContextAccessor.HttpContext.User = principal;
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var accessToken = new JwtSecurityToken(
                claims: claims,
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            var refreshToken = new JwtSecurityToken(
                claims: new List<Claim> { new Claim("id", user.Id.ToString()) },
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                signingCredentials: creds
            );
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);
            
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "unknown";

            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "RefreshToken");
            await _userManager.SetAuthenticationTokenAsync(user, "Default", "RefreshToken", refreshTokenString);
            
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
                    CreatedTime = user.CreatedTime,
                    Role = roleName,
                }
            };
        }

        public async Task<AppUser?> FindUserByRefreshTokenAsync(string provider, string tokenName, string tokenValue)
        {
            var tokenRepository = _unitOfWork.GetRepository<IdentityUserToken<string>>();
            if (tokenRepository == null)
                throw new NotSupportedException("IUnitOfWork does not provide a repository for IdentityUserToken<string>.");

            var userToken = await tokenRepository.Entities
                .Where(t => t.LoginProvider == provider && t.Name == tokenName && t.Value == tokenValue)
                .FirstOrDefaultAsync();

            if (userToken == null)
                return null; 

            var userRepository = _unitOfWork.GetRepository<AppUser>();
            if (userRepository == null)
                throw new NotSupportedException("IUnitOfWork does not provide a repository for AppUser.");

            return await userRepository.Entities
                .Where(u => u.Id == userToken.UserId && !u.DeletedTime.HasValue)  
                .FirstOrDefaultAsync();
        }
    }
}
