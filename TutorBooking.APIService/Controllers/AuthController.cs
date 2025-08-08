using App.Core.Base;
using App.DTOs.AuthDTOs;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        #region DI Constructor
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }
        #endregion  

        [HttpPost("sync-roles")]
        public async Task<IActionResult> SyncRoles()
        {
            var roleNames = await _authService.SyncRolesAsync();
            return Ok(new BaseResponseModel<IEnumerable<string>>(
                data: roleNames,
                message: "Đồng bộ roles thành công!"
            ));
        }
        
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole(CreateRoleRequest model)
        {
            await _authService.CreateRoleAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Tạo Role thành công!"
            ));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            await _authService.RegisterAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Đăng kí thành công!"
            ));
        }

        [HttpPatch("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmOTPRequest model)
        {
            await _authService.VerifyEmailAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Xác nhận email thành công!"
            ));
        }

        [HttpPost("resend-verification-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationEmail(EmailModel model)
        {
            await _authService.ResendVerificationEmailAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Đã gửi lại email xác nhận."
            ));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var res = await _authService.LoginAsync(request);
            return Ok(new BaseResponseModel<LoginResponse>(
                data: res,
                message: "Đăng nhập thành công!"
            ));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest model)
        {
            var res = await _authService.RefreshTokenAsync(model);
            return Ok(new BaseResponseModel<ResponseAuthModel>(
                data: res
            ));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(EmailModel model)
        {
            await _authService.RequestResetPasswordAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Đã gửi email xác nhận yêu cầu thay đổi mật khẩu."
            ));
        }

        [HttpPatch("confirm-reset-password")]
        public async Task<IActionResult> ConfirmResetPassword(ResetPasswordRequest model)
        {
            await _authService.ResetPasswordAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Xác nhận thay đổi mật khẩu thành công!"
            ));
        }

        [HttpPatch("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            await _authService.ResetPasswordAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Đã đặt lại mật khẩu thành công!"
            ));
        }

        [HttpDelete("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest model)
        {
            await _authService.LogoutAsync(model);
            return Ok(new BaseResponseModel<string>(
                message: "Đăng xuất thành công!"
            ));
        }

        [HttpPost("login-firebase-v2")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginFirebaseV2([FromBody]string token)
        {
            return Ok(await _authService.LoginFirebaseAsync(token));
        }

        [HttpPost("login-firebase")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginFirebase()
        {
            _logger.LogInformation("Firebase login request received. Headers: {Headers}", Request.Headers);
            var response = await _authService.LoginFirebaseAsync();
            _logger.LogInformation("Firebase login response: {@Response}", response);
            return Ok(response);
        }

    }
}
