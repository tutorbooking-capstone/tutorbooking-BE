using App.Core.Base;
using App.DTOs.RatingDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TutorBooking.APIService.EventHandlers;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.NotificationHubs;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/booking-slot-rating")]
    [ApiController]
    public class BookingSlotRatingController : ControllerBase
    {
        private readonly IBookingSlotRatingService _bookingSlotRatingService;
        private readonly NotificationEventHandler _notificationEventHandler;

        public BookingSlotRatingController(IBookingSlotRatingService bookingSlotRatingService, NotificationEventHandler notificationEventHandler)
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
        public async Task<IActionResult> GetTutorRating(string tutorId)
        {
            var response = await _bookingSlotRatingService.GetTutorRatingAsync(tutorId);
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
