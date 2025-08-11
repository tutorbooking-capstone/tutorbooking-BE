using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/disputes")]
    [ApiController]
    [Authorize]
    public class BookingDisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public BookingDisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        private async Task<Dictionary<string, object>> GetMetadataAsync() => 
            await _disputeService.GetDisputeMetadataAsync();

        #region Learner Endpoints
        [HttpPost]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
        {
            var result = await _disputeService.CreateDisputeAsync(request);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<BookingDisputeResponse>(
                data: result,
                additionalData: metadata,
                message: "Khiếu nại đã được tạo thành công. Bạn và gia sư có 24 giờ để trao đổi giải quyết."
            ));
        }

        [HttpPost("withdraw")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> WithdrawDispute([FromBody] WithdrawDisputeRequest request)
        {
            var result = await _disputeService.WithdrawDisputeAsync(request);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<BookingDisputeResponse>(
                data: result,
                additionalData: metadata,
                message: "Khiếu nại đã được rút lại thành công."
            ));
        }

        [HttpGet("learner")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> GetLearnerDisputes([FromQuery] bool? onlyActive = null)
        {
            var result = await _disputeService.GetLearnerDisputesAsync(onlyActive);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<List<BookingDisputeResponse>>(
                data: result,
                additionalData: metadata,
                message: "Danh sách khiếu nại của học viên."
            ));
        }

        [HttpGet("learner/{disputeId}")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> GetLearnerDisputeDetail([FromRoute] string disputeId)
        {
            var result = await _disputeService.GetDisputeDetailForLearnerAsync(disputeId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<DisputeDetailResponse>(
                data: result,
                additionalData: metadata,
                message: "Thông tin chi tiết khiếu nại."
            ));
        }
        #endregion

        #region Tutor Endpoints
        [HttpPost("respond")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> RespondToDispute([FromBody] RespondToDisputeRequest request)
        {
            var result = await _disputeService.RespondToDisputeAsync(request);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<BookingDisputeResponse>(
                data: result,
                additionalData: metadata,
                message: "Phản hồi khiếu nại thành công. Khiếu nại đã được chuyển cho nhân viên hệ thống xử lý."
            ));
        }

        [HttpGet("tutor")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTutorDisputes([FromQuery] bool? onlyActive = null)
        {
            var result = await _disputeService.GetTutorDisputesAsync(onlyActive);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<List<BookingDisputeResponse>>(
                data: result,
                additionalData: metadata,
                message: "Danh sách khiếu nại liên quan đến gia sư."
            ));
        }

        [HttpGet("tutor/{disputeId}")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTutorDisputeDetail([FromRoute] string disputeId)
        {
            var result = await _disputeService.GetDisputeDetailForTutorAsync(disputeId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<DisputeDetailResponse>(
                data: result,
                additionalData: metadata,
                message: "Thông tin chi tiết khiếu nại."
            ));
        }
        #endregion

        #region Staff Endpoints
        [HttpGet("staff")]
        [AuthorizeRoles(Role.Admin, Role.Staff)]
        public async Task<IActionResult> GetStaffDisputes()
        {
            var result = await _disputeService.GetDisputesForReviewAsync();
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<List<BookingDisputeResponse>>(
                data: result,
                additionalData: metadata,
                message: "Danh sách khiếu nại cần xử lý."
            ));
        }

        [HttpGet("staff/{disputeId}")]
        [AuthorizeRoles(Role.Admin, Role.Staff)]
        public async Task<IActionResult> GetStaffDisputeDetail([FromRoute] string disputeId)
        {
            var result = await _disputeService.GetDisputeDetailForStaffAsync(disputeId);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<DisputeDetailResponse>(
                data: result,
                additionalData: metadata,
                message: "Thông tin chi tiết khiếu nại."
            ));
        }

        [HttpPost("resolve")]
        [AuthorizeRoles(Role.Admin, Role.Staff)]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
        {
            var result = await _disputeService.ResolveDisputeAsync(request);
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<BookingDisputeResponse>(
                data: result,
                additionalData: metadata,
                message: "Khiếu nại đã được giải quyết thành công."
            ));
        }
        #endregion
        
        [HttpGet("metadata")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMetadata()
        {
            var metadata = await GetMetadataAsync();
            
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho luồng xử lý khiếu nại"
            ));
        }
    }
}