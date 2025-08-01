using App.Core.Base;
using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Text.Json;
using TutorBooking.APIService.Hubs.NotificationHubs;
using static Google.Apis.Requests.BatchRequest;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/booking-slot-rating")]
    [ApiController]
    public class BookingSlotRatingController : ControllerBase
    {
        private readonly IBookingSlotRatingService _bookingSlotRatingService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public BookingSlotRatingController(IBookingSlotRatingService bookingSlotRatingService, INotificationService notificationService, IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _bookingSlotRatingService = bookingSlotRatingService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRating(BookingSlotRatingRequest request)
        {
            var response = await _bookingSlotRatingService.CreateAsync(request);

            await _hubContext.SendNotificationToUsersAsync(_notificationService, new()
            {
                Content = new()
                {
                    NotificationPriority = App.Repositories.Models.Notifications.ENotificationPriority.Normal,
                    Title = "PUSH_ON_TUTOR_RATING_RECEIVED",
                    Content = "PUSH_ON_TUTOR_RATING_RECEIVED",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Id = response.Id,
                        BookingId = response.BookingId,
                        LearnerId = response.LearnerId,
                        AverageRating = (response.TeachingQuality + response.Attitude + response.Commitment) / 3
                    })
                },
                ReceiverUserIds = [response.TutorId]
            });

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
