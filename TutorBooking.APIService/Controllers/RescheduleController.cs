using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/reschedule-requests")]
    [ApiController]
    [Authorize]
    public class RescheduleController : ControllerBase
    {
        private readonly IRescheduleService _service;

        public RescheduleController(IRescheduleService service)
        {
            _service = service;
        }

        private async Task<Dictionary<string, object>> GetMetadataAsync() => 
            await _service.GetRescheduleMetadataAsync();

        [HttpPost]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> CreateRescheduleRequest([FromBody] CreateRescheduleRequest request)
        {
            var rescheduleRequest = await _service.CreateRescheduleRequestAsync(request);
            var metadata = await GetMetadataAsync();
            
            return CreatedAtAction(
                nameof(GetRescheduleRequestById), 
                new { requestId = rescheduleRequest.Id }, 
                new BaseResponseModel<RescheduleRequestResponse>(
                    data: rescheduleRequest,
                    additionalData: metadata,
                    message: "Yêu cầu thay đổi lịch đã được gửi."
                )
            );
        }

        [HttpGet]
        public async Task<IActionResult> GetRescheduleRequests([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10)
        {
            var requests = await _service.GetRescheduleRequestsAsync(pageIndex, pageSize);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<BasePaginatedList<RescheduleRequestResponse>>(
                data: requests,
                additionalData: metadata,
                message: "Lấy danh sách yêu cầu thay đổi lịch thành công."
            ));
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetRescheduleRequestById([FromRoute] string requestId)
        {
            var request = await _service.GetRescheduleRequestByIdAsync(requestId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<RescheduleRequestResponse>(
                data: request,
                additionalData: metadata,
                message: "Lấy thông tin yêu cầu thay đổi lịch thành công."
            ));
        }

        [HttpPost("{requestId}/accept")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> AcceptRescheduleRequest([FromRoute] string requestId)
        {
            var request = await _service.AcceptRescheduleRequestAsync(requestId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<RescheduleRequestResponse>(
                data: request,
                additionalData: metadata,
                message: "Đã chấp nhận yêu cầu thay đổi lịch."
            ));
        }

        [HttpPost("{requestId}/reject")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> RejectRescheduleRequest([FromRoute] string requestId, [FromBody] string? note)
        {
            var request = await _service.RejectRescheduleRequestAsync(requestId, note);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<RescheduleRequestResponse>(
                data: request,
                additionalData: metadata,
                message: "Đã từ chối yêu cầu thay đổi lịch."
            ));
        }

        [HttpDelete("{requestId}")]
        public async Task<IActionResult> CancelRescheduleRequest([FromRoute] string requestId)
        {
            var request = await _service.CancelRescheduleRequestAsync(requestId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<RescheduleRequestResponse>(
                data: request,
                additionalData: metadata,
                message: "Đã hủy yêu cầu thay đổi lịch."
            ));
        }

        [HttpGet("metadata")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRescheduleMetadata()
        {
            var metadata = await GetMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho yêu cầu thay đổi lịch"
            ));
        }
    }
}
