using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/tutor-bookings")]
    [ApiController]
    [Authorize]
    public class TutorBookingController : ControllerBase
    {
        private readonly ITutorBookingService _service;

        public TutorBookingController(ITutorBookingService service)
        {
            _service = service;
        }

        [HttpGet("time-slots")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetAllTimeSlotRequestsForTutor()
        {
            var requests = await _service.GetAllTimeSlotRequestsForTutorAsync();
            return Ok(new BaseResponseModel<List<LearnerInfoDTO>>(
                data: requests,
                message: "Danh sách học viên đã gửi yêu cầu"
            ));
        }

        [HttpGet("learners/{learnerId}/time-slots")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTimeSlotRequestByLearner([FromRoute] string learnerId)
        {
            var request = await _service.GetTimeSlotRequestByLearnerAsync(learnerId);
            return Ok(new BaseResponseModel<LearnerTimeSlotResponseDTO>(
                data: request,
                message: "Chi tiết yêu cầu khung giờ từ học viên"
            ));
        }

        [HttpPost("offers")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> CreateBookingOffer([FromBody] CreateTutorBookingOfferRequest request)
        {
            var offer = await _service.CreateBookingOfferAsync(request);
            return CreatedAtAction(nameof(GetBookingOfferById), new { offerId = offer.Id }, new BaseResponseModel<TutorBookingOfferResponse>(
                data: offer,
                message: "Tạo đề nghị gói học thành công."
            ));
        }

        [HttpGet("offers")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetAllBookingOffers()
        {
            var offers = await _service.GetAllBookingOffersByTutorAsync();
            return Ok(new BaseResponseModel<List<TutorBookingOfferResponse>>(
                data: offers,
                message: "Lấy danh sách đề nghị đã tạo thành công."
            ));
        }

        [HttpGet("offers/{offerId}", Name = "GetBookingOfferById")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetBookingOfferById([FromRoute] string offerId)
        {
            var offer = await _service.GetBookingOfferByIdForTutorAsync(offerId);
            return Ok(new BaseResponseModel<TutorBookingOfferResponse>(
                data: offer,
                message: "Lấy thông tin đề nghị thành công."
            ));
        }

        [HttpPut("offers/{offerId}")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> UpdateBookingOffer([FromRoute] string offerId, [FromBody] UpdateTutorBookingOfferRequest request)
        {
            var offer = await _service.UpdateBookingOfferAsync(offerId, request);
            return Ok(new BaseResponseModel<TutorBookingOfferResponse>(
                data: offer,
                message: "Cập nhật đề nghị thành công."
            ));
        }

        [HttpDelete("offers/{offerId}")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> DeleteBookingOffer([FromRoute] string offerId)
        {
            await _service.DeleteBookingOfferAsync(offerId);
            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Xóa đề nghị thành công."
            ));
        }
    }
}