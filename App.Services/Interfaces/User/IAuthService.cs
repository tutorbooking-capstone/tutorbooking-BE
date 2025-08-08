using App.DTOs.AuthDTOs;
using System.Runtime.InteropServices;

namespace App.Services.Interfaces.User
{
    public interface IAuthService
    {
        Task<IEnumerable<string>> SyncRolesAsync();
        Task CreateRoleAsync(CreateRoleRequest model);
        Task RegisterAsync(RegisterRequest model);
        Task VerifyEmailAsync(ConfirmOTPRequest model);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task RequestResetPasswordAsync(EmailModel model);
        Task ResetPasswordAsync(ResetPasswordRequest model);
        Task<ResponseAuthModel> RefreshTokenAsync(RefreshTokenRequest refreshTokenModel);
        Task LogoutAsync(RefreshTokenRequest model);
        Task SeedRegisterAsync(RegisterRequest model);
        Task<LoginResponse> LoginGoogleAsync(string credential);
        Task<LoginResponse> LoginFirebaseAsync([Optional] string? token);
        Task ResendVerificationEmailAsync(EmailModel model);
    }
}
