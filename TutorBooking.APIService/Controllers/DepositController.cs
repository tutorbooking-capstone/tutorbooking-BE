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
                // Kiểm tra xem rawData có phải là một JSON object không
                if (rawData == null)
                {
                    _logger.LogWarning("Received empty callback from PayOS");
                    return BadRequest(new { success = false, message = "Callback data is null" });
                }

                // Nếu rawData là Newtonsoft.Json.Linq.JObject hoặc System.Text.Json.JsonElement
                Dictionary<string, string>? callbackData = null;
                
                if (rawData is System.Text.Json.JsonElement jsonElement)
                {
                    callbackData = new Dictionary<string, string>();
                    
                    // Trích xuất dữ liệu từ JsonElement
                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        // Nếu property là một đối tượng phức tạp, chuyển nó thành JSON string
                        if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                            callbackData[property.Name] = property.Value.ToString();
                        else
                            callbackData[property.Name] = property.Value.ToString();
                    }
                }
                else
                {
                    // Thử chuyển đổi trực tiếp
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(rawData);
                        callbackData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>?>(json) 
                                        ?? new Dictionary<string, string>();
                    }
                    catch
                    {
                        _logger.LogWarning("Could not convert rawData to Dictionary<string, string>");
                    }
                }
                
                if (callbackData == null || !callbackData.Any())
                {
                    _logger.LogWarning("Failed to parse callback data from PayOS");
                    return BadRequest(new { success = false, message = "Invalid callback data format" });
                }
                
                _logger.LogInformation("Parsed PayOS callback: {Data}", string.Join(", ", callbackData.Select(kv => $"{kv.Key}={kv.Value}")));
                
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

        [HttpGet("return")]
        public async Task<IActionResult> ReturnFromPayment([FromQuery] string orderCode)
        {
            // Kiểm tra và cập nhật trạng thái
            var updated = await _depositService.CheckAndUpdateDepositStatusAsync(orderCode);
            
            // Lấy thông tin đơn hàng
            var depositRequest = await _depositService.GetDepositRequestByIdAsync(orderCode);
            
            return Ok(new BaseResponseModel<object>(
                data: new {
                    updated = updated,
                    deposit = depositRequest
                },
                message: "Đã quay lại từ trang thanh toán"
            ));
        }
    }
}