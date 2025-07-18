using App.Core.Base;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(
            IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserWallet([FromQuery] string? userId = null)
        {
            // ### For Production: Uncomment the authorization check below
            // if (!string.IsNullOrEmpty(userId) && !User.IsInRole(Role.Admin.ToStringRole()) && !User.IsInRole(Role.Staff.ToStringRole()))
            //     throw new ErrorException(
            //          StatusCodes.Status403Forbidden, 
            //          ErrorCode.Forbidden, 
            //          "Bạn không có quyền xem thông tin ví của người dùng khác.");

            var wallet = await _walletService.GetWalletAsync(userId);
            return Ok(new BaseResponseModel<WalletResponse>(
                data: wallet,
                message: "Thông tin ví của người dùng."
            ));
        }

        [HttpGet("transactions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserTransactions(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? userId = null)
        {
            // If a specific userId is requested, check for Admin/Staff role.
            // If userId is not provided, it's a request for the current user's own transactions, which is allowed.
            // if (!string.IsNullOrEmpty(userId) && !User.IsInRole(Role.Admin.ToStringRole()) && !User.IsInRole(Role.Staff.ToStringRole()))
            //     throw new ErrorException(
            //         StatusCodes.Status403Forbidden, 
            //         ErrorCode.Forbidden, 
            //         "Bạn không có quyền xem lịch sử giao dịch của người dùng khác.");

            var transactions = await _walletService.GetTransactionsAsync(userId, page, pageSize);
            return Ok(new BaseResponseModel<BasePaginatedList<TransactionResponse>>(
                data: transactions,
                message: "Lịch sử giao dịch của người dùng."
            ));
        }

        [HttpGet("system")]
        //[AuthorizeRoles(Role.Admin, Role.Staff)]   ### Just Comment For Test
        [AllowAnonymous]
        public async Task<IActionResult> GetSystemWallet()
        {
            var systemWallet = await _walletService.GetSystemWalletAsync();
            return Ok(new BaseResponseModel<WalletResponse>(
                data: systemWallet,
                message: "Thông tin ví hệ thống."
            ));
        }

        // [HttpPost("create-for-all")]
        // //[AuthorizeRoles(Role.Admin)]    ### Just Comment For Test
        // [AllowAnonymous]
        // public async Task<IActionResult> CreateWalletsForAllUsers()
        // {
        //     var result = await _walletService.CreateWalletForAllUsersAsync();
        //     return Ok(new BaseResponseModel<object>(
        //         data: new { success = result },
        //         message: "Hoàn tất quá trình tạo ví cho người dùng."
        //     ));
        // }

        // [HttpPost("deposit")]
        // public async Task<ActionResult<PayosPaymentResponse>> CreateDeposit([FromBody] CreateDepositRequest request)
        // {
        //     var depositRequest = await _depositService.CreateDepositRequestAsync(request.Amount);
        //     var paymentResponse = await _depositService.GeneratePayosPaymentAsync(depositRequest.Id);
        //     return Ok(paymentResponse);
        // }

        // [HttpGet("deposits")]
        // public async Task<ActionResult<BasePaginatedList<DepositRequestResponse>>> GetUserDeposits(
        //     [FromQuery] int page = 1,
        //     [FromQuery] int pageSize = 10)
        // {
        //     var deposits = await _depositService.GetUserDepositHistoryAsync(page, pageSize);
        //     return Ok(deposits);
        // }

        // [HttpGet("deposits/all")]
        // [Authorize(Roles = "Admin,Staff")]
        // public async Task<ActionResult<BasePaginatedList<DepositRequestResponse>>> GetAllDeposits(
        //     [FromQuery] int page = 1,
        //     [FromQuery] int pageSize = 10)
        // {
        //     var deposits = await _depositService.GetAllDepositHistoryAsync(page, pageSize);
        //     return Ok(deposits);
        // }
    }
}