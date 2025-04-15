using App.DTOs.AuthDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces.User
{
    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokens(AppUser user, string role);
        Task<AppUser?> FindUserByRefreshTokenAsync(string provider, string tokenName, string tokenValue);
    }
}
