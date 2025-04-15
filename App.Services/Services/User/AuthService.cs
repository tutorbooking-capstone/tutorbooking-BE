using App.Core.Base;
using App.Core.Constants;
using App.Core.Utils;
using App.DTOs.AuthDTOs;
using App.Repositories.Models;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using Microsoft.Extensions.Logging;

namespace App.Services.Services.User
{
    public class AuthService : IAuthService
    {
        #region DI Constructor
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<AppUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService, 
            IConfiguration configuration, 
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
        }
        #endregion
        
        #region Private Service
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        #endregion

        public async Task CreateRoleAsync(CreateRoleRequest model)
        {
            if (await _roleManager.RoleExistsAsync(model.RoleName))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò đã tồn tại!");
            }
            
            var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
            
            if (!result.Succeeded)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, 
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo vai trò");
            }
        }

        public async Task RegisterAsync(RegisterRequest model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null && !existingUser.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Email đã được đăng ký!");

            if (model.Password != model.ConfirmPassword)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Xác nhận mật khẩu không đúng!");

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Vai trò không tồn tại!");

            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));

            string otp = GenerateOtp();
            int otpCode = int.Parse(otp);

            var newUser = new AppUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = model.Email,
                UserName = model.Email,
                NormalizedEmail = _userManager.KeyNormalizer.NormalizeEmail(model.Email),
                NormalizedUserName = _userManager.KeyNormalizer.NormalizeName(model.Email), 
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = passwordHasher.HashPassword(null, model.Password),
                PhoneNumberConfirmed = true,
                EmailConfirmed = false,
                CreatedTime = DateTime.UtcNow,
                EmailCode = otpCode,
                CodeGeneratedTime = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, 
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo tài khoản");
            }

            result = await _userManager.AddToRoleAsync(newUser, model.RoleName);
            if (!result.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, 
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể gán vai trò");
            }

            await _emailService.SendEmailAsync(model.Email, "Xác thực tài khoản",
                $"Chào {model.Email},<br><br> Mã OTP của bạn để xác thực tài khoản là: <strong>{otp}</strong>.<br> Vui lòng nhập mã này trong vòng 5 phút để hoàn tất đăng ký.");
        }

        public async Task VerifyOtpAsync(ConfirmOTPRequest model, bool isResetPassword)
        {
            AppUser? user = await _userManager.FindByEmailAsync(model.Email)
                ?? throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Vui lòng kiểm tra email của bạn");

            if (user.EmailCode == null || user.EmailCode.ToString() != model.OTP)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "OTP không hợp lệ");

            if (!user.CodeGeneratedTime.HasValue || DateTime.UtcNow > user.CodeGeneratedTime.Value.AddMinutes(5))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "OTP đã hết hạn");

            if (!isResetPassword)
            {
                string? token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
            }

            user.EmailCode = null;
            user.CodeGeneratedTime = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Không thể cập nhật thông tin người dùng");
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            
            if (user == null || user.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ResponseCodeConstants.BADREQUEST, 
                    "Không tìm thấy tài khoản");

            if (user.EmailConfirmed == false || user.PhoneNumberConfirmed == false)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ResponseCodeConstants.BADREQUEST, 
                    "Tài khoản chưa được xác thực!");

            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));
            var hashedInputPassword = passwordHasher.HashPassword(null, model.Password);

            if (hashedInputPassword != user.PasswordHash)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Không tìm thấy tài khoản");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "unknown";
            
            var tokenResponse = await _tokenService.GenerateTokens(user, roleName);
            var loginResponse = new LoginResponse
            {
                Token = tokenResponse,
                Role = roleName,
            };

            return loginResponse;
        }

        public async Task ForgotPasswordAsync(EmailModel model)
        {
            AppUser? user = await _userManager.FindByEmailAsync(model.Email)
                ?? throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Vui lòng kiểm tra email của bạn");

            if (!await _userManager.IsEmailConfirmedAsync(user))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Vui lòng kiểm tra email của bạn");

            string OTP = GenerateOtp();
            user.EmailCode = int.Parse(OTP);
            user.CodeGeneratedTime = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Không thể lưu OTP, vui lòng thử lại sau.");

            await _emailService.SendEmailAsync(
                model.Email, 
                "Đặt lại mật khẩu", 
                $"Vui lòng xác nhận tài khoản của bạn, OTP của bạn là: <div class='otp'>{OTP}</div>");
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest model)
        {
            AppUser? user = await _userManager.FindByEmailAsync(model.Email)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy user");

            if (!await _userManager.IsEmailConfirmedAsync(user))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Vui lòng kiểm tra email của bạn");

            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));
            string hashedNewPassword = passwordHasher.HashPassword(user, model.Password);

            user.PasswordHash = hashedNewPassword;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    updateResult.Errors.FirstOrDefault()?.Description ?? "Không thể cập nhật mật khẩu");
        }

        public async Task<ResponseAuthModel> RefreshTokenAsync(RefreshTokenRequest refreshTokenModel)
        {
            AppUser? user = await _tokenService.FindUserByRefreshTokenAsync("Default", "RefreshToken", refreshTokenModel.RefreshToken);

            if (user == null)
                throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "Refresh token không hợp lệ hoặc đã hết hạn.");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "User";

            var tokenResponse = await _tokenService.GenerateTokens(user, roleName);

            return new ResponseAuthModel
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenType = "JWT",
                AuthType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes")),
                User = new UserInfo
                {
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList(),
                }
            };
        }

        public async Task LogoutAsync(RefreshTokenRequest model)
        {
            AppUser? user = await _tokenService.FindUserByRefreshTokenAsync("Default", "RefreshToken", model.RefreshToken);

            if (user == null)
            {
                return;
            }

            var result = await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "RefreshToken");

            if (!result.Succeeded)
                _logger.LogError($"Failed to remove refresh token for user {user.Id}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

}
