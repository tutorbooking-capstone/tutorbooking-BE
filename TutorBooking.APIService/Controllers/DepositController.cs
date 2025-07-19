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
    public class DepositController : ControllerBase
    {
        private readonly IDepositService _depositService;
        private readonly ILogger<DepositController> _logger;

        public DepositController(
            IDepositService depositService,
            ILogger<DepositController> logger)
        {
            _depositService = depositService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositRequest request)
        {
            var depositRequest = await _depositService.CreateDepositRequestAsync(request.Amount);
            var paymentResponse = await _depositService.GeneratePayosPaymentAsync(depositRequest.Id);
            
            return Ok(new BaseResponseModel<object>(
                data: new { 
                    deposit = depositRequest,
                    payment = paymentResponse
                },
                message: "Tạo yêu cầu nạp tiền thành công"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepositRequest(string id)
        {
            var depositRequest = await _depositService.GetDepositRequestByIdAsync(id);
            return Ok(new BaseResponseModel<DepositRequestResponse>(
                data: depositRequest,
                message: "Thông tin yêu cầu nạp tiền"
            ));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetUserDepositHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var history = await _depositService.GetUserDepositHistoryAsync(page, pageSize);
            return Ok(new BaseResponseModel<BasePaginatedList<DepositRequestResponse>>(
                data: history,
                message: "Lịch sử nạp tiền"
            ));
        }

        [HttpGet("all")]
        [AuthorizeRoles(Role.Admin, Role.Staff)]
        public async Task<IActionResult> GetAllDepositHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var history = await _depositService.GetAllDepositHistoryAsync(page, pageSize);
            return Ok(new BaseResponseModel<BasePaginatedList<DepositRequestResponse>>(
                data: history,
                message: "Tất cả lịch sử nạp tiền"
            ));
        }

        [HttpPost("check-status/{id}")]
        public async Task<IActionResult> CheckDepositStatus(string id)
        {
            var updated = await _depositService.CheckAndUpdateDepositStatusAsync(id);
            var depositRequest = await _depositService.GetDepositRequestByIdAsync(id);
            
            return Ok(new BaseResponseModel<object>(
                data: new {
                    updated = updated,
                    deposit = depositRequest
                },
                message: updated ? "Đã cập nhật trạng thái nạp tiền" : "Không có thay đổi trạng thái"
            ));
        }

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PayosCallback([FromBody] object rawData)
        {
            // Log raw request body
            _logger.LogInformation("Raw PayOS callback data: {RawData}", rawData);
            
            try
            {
                var callbackData = rawData as Dictionary<string, string>;
                
                if (callbackData == null || !callbackData.Any())
                {
                    // Try to read request body manually
                    using var reader = new StreamReader(Request.Body);
                    Request.Body.Position = 0; // Reset position
                    var body = await reader.ReadToEndAsync();
                    _logger.LogWarning("Received empty or invalid callback from PayOS. Raw body: {Body}", body);
                    
                    return BadRequest(new { success = false, message = "Callback data is null or empty or in wrong format." });
                }
                
                _logger.LogInformation("Received PayOS callback: {Data}", string.Join(", ", callbackData.Select(kv => $"{kv.Key}={kv.Value}")));
                
                var result = await _depositService.ProcessPayosCallbackAsync(callbackData);
                
                if (result)
                    return Ok(new { success = true });
                else
                    return BadRequest(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS callback");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}