using App.Core.Base;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models.Payment;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/fees")]
    [ApiController]
    [AuthorizeRoles(Role.Manager)]
    public class FeeController : ControllerBase
    {
        private readonly IFeeService _feeService;

        public FeeController(IFeeService feeService)
        {
            _feeService = feeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFees()
        {
            var fees = await _feeService.GetAllActiveFeesAsync();
            var metadata = await _feeService.GetFeeMetadataAsync();

            return Ok(new BaseResponseModel<List<FeeConfig>>(
                data: fees,
                additionalData: metadata,
                message: "Danh sách phí hiện tại"
            ));
        }

        [HttpGet("{feeCode}")]
        public async Task<IActionResult> GetFeeByCode(string feeCode)
        {
            var feeInfo = await _feeService.GetFeeInfoByCodeAsync(feeCode);
            var metadata = await _feeService.GetFeeMetadataAsync();

            if (feeInfo == null)
            {
                return NotFound(new BaseResponseModel<object>(
                    data: null,
                    message: $"Không tìm thấy mã phí: {feeCode}"
                ));
            }

            return Ok(new BaseResponseModel<object>(
                data: feeInfo,
                additionalData: metadata,
                message: $"Thông tin phí {feeCode}"
            ));
        }

        [HttpPost("setup")]
        public async Task<IActionResult> SetupFeeValue([FromBody] SetupFeeRequest request)
        {
            // Kiểm tra feeCode có hợp lệ không
            var metadata = await _feeService.GetFeeMetadataAsync();
            var allFeeCodes = (metadata["FeeCodes"] as List<string>) ?? new List<string>();
            
            if (!allFeeCodes.Contains(request.FeeCode))
            {
                return BadRequest(new BaseResponseModel<object>(
                    data: null,
                    message: $"Mã phí không hợp lệ: {request.FeeCode}"
                ));
            }

            var result = await _feeService.CreateOrUpdateFeeConfigAsync(
                request.FeeCode,
                request.Value,
                request.Type,
                request.Description
            );

            return Ok(new BaseResponseModel<FeeConfig>(
                data: result,
                additionalData: metadata,
                message: $"Thiết lập giá trị cho phí {request.FeeCode} thành công"
            ));
        }

        // [HttpGet("calculate")]
        // public async Task<IActionResult> CalculateFee([FromQuery] string feeCode, [FromQuery] decimal amount)
        // {
        //     if (string.IsNullOrEmpty(feeCode))
        //     {
        //         return BadRequest(new BaseResponseModel<object>(
        //             data: null,
        //             message: "Mã phí không được để trống"
        //         ));
        //     }

        //     var fee = await _feeService.GetActiveFeeByCodeAsync(feeCode);
        //     var calculatedAmount = await _feeService.CalculateFeeAsync(feeCode, amount);

        //     return Ok(new BaseResponseModel<object>(
        //         data: new
        //         {
        //             FeeCode = feeCode,
        //             OriginalAmount = amount,
        //             CalculatedFee = calculatedAmount,
        //             FeeConfig = fee
        //         },
        //         message: $"Kết quả tính phí {feeCode}"
        //     ));
        // }
    }

}