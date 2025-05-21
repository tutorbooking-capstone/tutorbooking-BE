using App.Core.Base;
using App.Core.Constants;
using App.Core.Utils;
using App.DTOs.AuthDTOs;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using Microsoft.Extensions.Logging;
using App.Repositories.Models.User;
using App.Repositories.UoW;

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
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IConfiguration configuration,
            ITokenService tokenService,
            ILogger<AuthService> logger,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager)); ;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
            _unitOfWork = unitOfWork;
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
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò đã tồn tại!");

            var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));

            if (!result.Succeeded)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo vai trò");
        }

        public async Task RegisterAsync(RegisterRequest model)
        {
            if (model.Password != model.ConfirmPassword)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Xác nhận mật khẩu không đúng!");

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
            newUser.TrackCreate(newUser.Id);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo tài khoản");

            result = await _userManager.AddToRoleAsync(newUser, Role.Learner.ToString());
            if (!result.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể gán vai trò");
            }

            // Create Learner entity
            var learner = newUser.BecomeLearner(newUser.Id);
            _unitOfWork.GetRepository<Learner>().Insert(learner);
            await _unitOfWork.SaveAsync();

            string greeting = $"Chào {model.Email},";
            string mainMessage = $@"
                Cảm ơn bạn đã đăng ký tài khoản.
                <br>Mã OTP của bạn để xác thực tài khoản là: <div class='otp-code'>{otp}</div>
                <br>Vui lòng nhập mã này trong vòng 5 phút để hoàn tất đăng ký.";

            await _emailService.SendEmailAsync(
                model.Email,
                "Xác thực tài khoản",
                greeting,
                mainMessage);
        }

        public async Task SeedRegisterAsync(RegisterRequest model)
        {
            if (model.Password != model.ConfirmPassword)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Xác nhận mật khẩu không đúng!");

            var passwordHasher = new FixedSaltPasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions()));

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
                EmailConfirmed = true, // Set to true directly, no verification needed
                CreatedTime = DateTime.UtcNow
                // No EmailCode or CodeGeneratedTime needed
            };

            var result = await _userManager.CreateAsync(newUser);
            newUser.TrackCreate(newUser.Id);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể tạo tài khoản");

            result = await _userManager.AddToRoleAsync(newUser, Role.Learner.ToString());
            if (!result.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    result.Errors.FirstOrDefault()?.Description ?? "Không thể gán vai trò");
            }

            // Create Learner entity
            var learner = newUser.BecomeLearner(newUser.Id);
            _unitOfWork.GetRepository<Learner>().Insert(learner);
            await _unitOfWork.SaveAsync();
            
            // No email sending
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
            if (_userManager == null)
            {
                _logger.LogError("UserManager is null!");
                throw new InvalidOperationException("UserManager is not initialized.");
            }
            
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null || user.DeletedTime.HasValue)
            {
                _logger.LogWarning("Login attempt for non-existent or deleted user: {Username}", model.Username);
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            if (user.EmailConfirmed == false)
            {
                _logger.LogWarning("Login attempt for unconfirmed email user: {Username}", model.Username);
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.BADREQUEST,
                    "Tài khoản chưa được xác thực!");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogError("User {Username} has no password hash.", model.Username);
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            var passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Failed login attempt (incorrect password) for user: {Username}", model.Username);
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenResponse = await _tokenService.GenerateTokens(user, roles);
            
            var loginResponse = new LoginResponse
            {
                Token = tokenResponse,
                Role = tokenResponse.User?.Role ?? roles.FirstOrDefault() ?? "unknown",
                Roles = roles.ToList()
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

            string greeting = $"Chào {model.Email},";
            string mainMessage = $@"
                Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.
                <br>Mã OTP của bạn là: <div class='otp-code'>{OTP}</div>
                <br>Vui lòng sử dụng mã này trong vòng 5 phút để đặt lại mật khẩu.";

            await _emailService.SendEmailAsync(
                model.Email,
                "Yêu cầu đặt lại mật khẩu",
                greeting,
                mainMessage);
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
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "Refresh token không hợp lệ hoặc đã hết hạn.");

            var roles = await _userManager.GetRolesAsync(user);

            var tokenResponse = await _tokenService.GenerateTokens(user, roles);

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
                return;

            var result = await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "RefreshToken");

            if (!result.Succeeded)
                _logger.LogError($"Failed to remove refresh token for user {user.Id}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public async Task<IEnumerable<string>> SyncRolesAsync()
        {
            var existingRoles = _roleManager.Roles.ToList();
            var roleNames = Enum.GetNames(typeof(Role)).ToHashSet();

            var rolesToDelete = existingRoles
                .Where(role => role.Name != null && !roleNames.Contains(role.Name))
                .ToList();

            foreach (var role in rolesToDelete)
            {
                await _roleManager.DeleteAsync(role);
            }

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            return roleNames;
        }
    }

}
