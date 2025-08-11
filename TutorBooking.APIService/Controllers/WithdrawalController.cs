// TutorBooking.APIService/Controllers/WithdrawalController.cs

using App.Core.Base;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorBooking.APIService.EventHandlers;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/withdrawals")]
    [ApiController]
    [Authorize]
    public class WithdrawalController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly IBankAccountService _bankAccountService;
        private readonly PushNotificationEventHandler _notificationEventHandler;

        public WithdrawalController(
            IWithdrawalService withdrawalService,
            IBankAccountService bankAccountService,
            PushNotificationEventHandler notificationEventHandler)
        {
            _withdrawalService = withdrawalService;
            _bankAccountService = bankAccountService;
            _notificationEventHandler = notificationEventHandler;
        }

        #region Withdrawal Endpoints
        [HttpPost]
        public async Task<IActionResult> CreateWithdrawalRequest([FromBody] CreateWithdrawalRequest request)
        {
            var result = await _withdrawalService.CreateWithdrawalRequestAsync(request);
            return Ok(new BaseResponseModel<WithdrawalRequestResponse>(
                data: result,
                message: "Yêu cầu rút tiền đã được tạo thành công"
            ));
        }

        [HttpGet]
        public async Task<IActionResult> GetWithdrawalRequests(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] WithdrawalRequestStatus? status = null)
        {
            var metadata = await _withdrawalService.GetWithdrawalMetadataAsync();
            var result = await _withdrawalService.GetWithdrawalRequestsAsync(page, pageSize, status);
            
            return Ok(new BaseResponseModel<BasePaginatedList<WithdrawalRequestResponse>>(
                data: result,
                additionalData: metadata,
                message: "Danh sách yêu cầu rút tiền"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWithdrawalRequestById(string id)
        {
            var result = await _withdrawalService.GetWithdrawalRequestByIdAsync(id);
            return Ok(new BaseResponseModel<WithdrawalRequestResponse>(
                data: result,
                message: "Chi tiết yêu cầu rút tiền"
            ));
        }

        [HttpPost("process")]
        [AuthorizeRoles(Role.Admin, Role.Staff, Role.Manager)]
        public async Task<IActionResult> ProcessWithdrawal([FromBody] ProcessWithdrawalRequest request)
        {
            var result = await _withdrawalService.ProcessWithdrawalAsync(request);
            return Ok(new BaseResponseModel<WithdrawalRequestResponse>(
                data: result,
                message: "Yêu cầu rút tiền đã được xử lý thành công"
            ));
        }

        [HttpPost("reject")]
        [AuthorizeRoles(Role.Admin, Role.Staff, Role.Manager)]
        public async Task<IActionResult> RejectWithdrawal([FromBody] RejectWithdrawalRequest request)
        {
            var result = await _withdrawalService.RejectWithdrawalAsync(request);
            return Ok(new BaseResponseModel<WithdrawalRequestResponse>(
                data: result,
                message: "Yêu cầu rút tiền đã bị từ chối"
            ));
        }
        #endregion

        #region Bank Account Endpoints
        [HttpPost("bank-accounts")]
        public async Task<IActionResult> CreateBankAccount([FromBody] BankAccountRequest request)
        {
            var result = await _bankAccountService.CreateBankAccountAsync(request);
            return CreatedAtAction(nameof(GetBankAccountById), new { id = result.Id }, new BaseResponseModel<BankAccountResponse>(
                data: result,
                message: "Tài khoản ngân hàng đã được tạo thành công"
            ));
        }

        [HttpGet("bank-accounts")]
        public async Task<IActionResult> GetBankAccounts()
        {
            var result = await _bankAccountService.GetUserBankAccountsAsync();
            return Ok(new BaseResponseModel<List<BankAccountResponse>>(
                data: result,
                message: "Danh sách tài khoản ngân hàng"
            ));
        }

        [HttpGet("bank-accounts/{id}")]
        public async Task<IActionResult> GetBankAccountById(string id)
        {
            var result = await _bankAccountService.GetBankAccountByIdAsync(id);
            return Ok(new BaseResponseModel<BankAccountResponse>(
                data: result,
                message: "Chi tiết tài khoản ngân hàng"
            ));
        }

        [HttpPut("bank-accounts/{id}")]
        public async Task<IActionResult> UpdateBankAccount(string id, [FromBody] BankAccountRequest request)
        {
            var result = await _bankAccountService.UpdateBankAccountAsync(id, request);
            return Ok(new BaseResponseModel<BankAccountResponse>(
                data: result,
                message: "Tài khoản ngân hàng đã được cập nhật thành công"
            ));
        }

        [HttpDelete("bank-accounts/{id}")]
        public async Task<IActionResult> DeleteBankAccount(string id)
        {
            await _bankAccountService.DeleteBankAccountAsync(id);
            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Tài khoản ngân hàng đã được xóa thành công"
            ));
        }
        #endregion
    }
}