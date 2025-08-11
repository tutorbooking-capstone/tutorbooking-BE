using App.Core.Base;
using App.DTOs.RatingDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorBooking.APIService.EventHandlers;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/booking-slot-rating")]
    [ApiController]
    public class BookingSlotRatingController : ControllerBase
    {
        private readonly IBookingSlotRatingService _bookingSlotRatingService;
        private readonly PushNotificationEventHandler _notificationEventHandler;

        public BookingSlotRatingController(IBookingSlotRatingService bookingSlotRatingService, PushNotificationEventHandler notificationEventHandler)
        {
            _bookingSlotRatingService = bookingSlotRatingService;
            _notificationEventHandler = notificationEventHandler;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRating(BookingSlotRatingRequest request)
        {
            var response = await _bookingSlotRatingService.CreateAsync(request);

            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpGet("tutor/{tutorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutorRating(string tutorId, int page =1, int size = 10)
        {
            var response = await _bookingSlotRatingService.GetTutorRatingAsync(tutorId, page, size);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _bookingSlotRatingService.GetByIdAsync(id);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpGet("booking/{bookingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByBookingId(string bookingId)
        {
            var response = await _bookingSlotRatingService.GetByBookingIdAsync(bookingId);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateRating(BookingSlotRatingUpdateRequest request)
        {
            await _bookingSlotRatingService.UpdateAsync(request);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
            ));
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteRating(string id)
        {
            await _bookingSlotRatingService.DeleteAsync(id);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
            ));
        }
    }
}
