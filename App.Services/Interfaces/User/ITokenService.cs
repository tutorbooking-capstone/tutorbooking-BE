﻿using App.DTOs.AuthDTOs;
using App.Repositories.Models.User;

namespace App.Services.Interfaces.User
{
    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokens(AppUser user, IList<string> roles);
        Task<AppUser?> FindUserByRefreshTokenAsync(string provider, string tokenName, string tokenValue);
    }
}
