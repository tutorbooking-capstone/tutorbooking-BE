//using App.Core.Base;
//using App.Core.Constants;
//using App.Services.Interfaces.User;
//using App.DTOs.AuthDTOs;
//using Microsoft.AspNetCore.Mvc;

//namespace App.TestAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AuthController : ControllerBase
//    {
//        #region DI Constructor
//        private readonly IAuthService _authService;

//        public AuthController(IAuthService authService)
//        {
//            _authService = authService;
//        }
//        #endregion  

//        [HttpPost("create-role")]
//        public async Task<IActionResult> CreateRole(CreateRoleRequest model)
//        {
//            await _authService.CreateRoleAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Tạo Role thành công!"
//            ));
//        }

//        [HttpPost("register")]
//        public async Task<IActionResult> Register(RegisterRequest model)
//        {
//            await _authService.RegisterAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Đăng kí thành công!"
//            ));
//        }

//        [HttpPatch("confirm-email")]
//        public async Task<IActionResult> ConfirmEmail(ConfirmOTPRequest model)
//        {
//            await _authService.VerifyOtpAsync(model, false);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Xác nhận email thành công!"
//            ));
//        }

//        [HttpPost("login")]
//        public async Task<IActionResult> Login(LoginRequest request)
//        {
//            var res = await _authService.LoginAsync(request);
//            return Ok(new BaseResponseModel<LoginResponse>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: res,
//                message: "Đăng nhập thành công!"
//            ));
//        }

//        [HttpPost("refresh-token")]
//        public async Task<IActionResult> RefreshToken(RefreshTokenRequest model)
//        {
//            var res = await _authService.RefreshTokenAsync(model);
//            return Ok(new BaseResponseModel<ResponseAuthModel>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                data: res
//            ));
//        }

//        [HttpPost("forgot-password")]
//        public async Task<IActionResult> ForgotPassword(EmailModel model)
//        {
//            await _authService.ForgotPasswordAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Đã gửi email xác nhận yêu cầu thay đổi mật khẩu."
//            ));
//        }

//        [HttpPatch("confirm-reset-password")]
//        public async Task<IActionResult> ConfirmResetPassword(ConfirmOTPRequest model)
//        {
//            await _authService.VerifyOtpAsync(model, true);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Xác nhận thay đổi mật khẩu thành công!"
//            ));
//        }

//        [HttpPatch("reset-password")]
//        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
//        {
//            await _authService.ResetPasswordAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Đã đặt lại mật khẩu thành công!"
//            ));
//        }

//        [HttpDelete("logout")]
//        public async Task<IActionResult> Logout(RefreshTokenRequest model)
//        {
//            await _authService.LogoutAsync(model);
//            return Ok(new BaseResponseModel<string>(
//                statusCode: StatusCodes.Status200OK,
//                code: ResponseCodeConstants.SUCCESS,
//                message: "Đăng xuất thành công!"
//            ));
//        }
//    }
//}
