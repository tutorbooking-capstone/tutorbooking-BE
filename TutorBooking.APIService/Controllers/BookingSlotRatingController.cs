using App.Core.Base;
using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using static Google.Apis.Requests.BatchRequest;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/booking-slot-rating")]
    [ApiController]
    public class BookingSlotRatingController : ControllerBase
    {
        private readonly IBookingSlotRatingService _bookingSlotRatingService;

        public BookingSlotRatingController(IBookingSlotRatingService bookingSlotRatingService)
        {
            _bookingSlotRatingService = bookingSlotRatingService;
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
