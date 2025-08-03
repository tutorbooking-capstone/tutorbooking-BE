using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TutorBooking.APIService.Hubs.NotificationHubs;
using static Google.Apis.Requests.BatchRequest;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/learner-bookings")]
    [ApiController]
    [Authorize]
    public class LearnerBookingController : ControllerBase
    {
        private readonly ILearnerBookingService _service;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
        private readonly IUserService _userService;

        public LearnerBookingController(ILearnerBookingService service, INotificationService notificationService, IHubContext<NotificationHub, INotificationClient> hubContext, IUserService userService)
        {
            _service = service;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _userService = userService;
        }

        [HttpPut("time-slots")]
        public async Task<IActionResult> UpdateTimeSlotRequests(
            [FromBody] LearnerTimeSlotRequestDTO request)
        {
            await _service.UpdateTimeSlotRequestsAsync(request);

            await _hubContext.SendNotificationToUsersAsync(_notificationService, new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "PUSH_ON_TUTOR_RECEIVED_TIME_SLOT_REQUEST",
                    Content = "PUSH_ON_TUTOR_RECEIVED_TIME_SLOT_REQUEST_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        ExpectedStartDate = request.ExpectedStartDate,
                        LessonId = request.LessonId,
                        SenderId = _userService.GetCurrentUserId(),
                    }, new JsonSerializerOptions {WriteIndented = false})
                },
                ReceiverUserIds = [request.TutorId]
            });

            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Cập nhật yêu cầu khung giờ thành công!"
            ));
        }

        [HttpDelete("tutors/{tutorId}/time-slots")]
        public async Task<IActionResult> DeleteTimeSlotRequests(
            [FromRoute] string tutorId)
        {
            await _service.DeleteTimeSlotRequestsAsync(tutorId);
            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Xóa toàn bộ yêu cầu khung giờ thành công!"
            ));
        }

        [HttpGet("tutors/{tutorId}/time-slots")]
        public async Task<IActionResult> GetTimeSlotRequestByTutor([FromRoute] string tutorId)
        {
            var request = await _service.GetTimeSlotRequestByTutorAsync(tutorId);
            return Ok(new BaseResponseModel<LearnerTimeSlotResponseDTO>(
                data: request,
                message: "Thông tin chi tiết yêu cầu khung giờ"
            ));
        }

        [HttpGet("offers")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> GetBookingOffers()
        {
            var offers = await _service.GetBookingOffersForLearnerAsync();
            return Ok(new BaseResponseModel<List<TutorBookingOfferResponse>>(
                data: offers,
                message: "Lấy danh sách gói học được đề nghị thành công."
            ));
        }

        [HttpGet("offers/{offerId}")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> GetBookingOfferById([FromRoute] string offerId)
        {
            var offer = await _service.GetBookingOfferByIdForLearnerAsync(offerId);
            return Ok(new BaseResponseModel<TutorBookingOfferResponse>(
                data: offer,
                message: "Lấy thông tin chi tiết gói học được đề nghị thành công."
            ));
        }

        [HttpGet("list-tutors-request")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> GetAllTimeSlotRequestsForLearner()
        {
            var tutors = await _service.GetAllTimeSlotRequestsForLearnerAsync();
            return Ok(new BaseResponseModel<List<TutorInfoDTO>>(
                data: tutors,
                message: "Danh sách gia sư đã gửi yêu cầu"
            ));
        }

        [HttpPost("accept-offer")]
        [Authorize(Roles = "Learner")]
        public async Task<ActionResult<BookingResponse>> AcceptOffer(AcceptOfferRequest request)
        {
            var result = await _service.AcceptTutorOfferAsync(request);
            await _hubContext.SendNotificationToUsersAsync(_notificationService, new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "PUSH_ON_LEARNER_ACCEPT_OFFER",
                    Content = "PUSH_ON_LEARNER_ACCEPT_OFFER_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Id = result.Id,
                        LessonName = result.LessonName,
                        SenderId = result.LearnerId,
                    }, new JsonSerializerOptions { WriteIndented = false })
                },
                ReceiverUserIds =[result.TutorId]
            });

            return Ok(result);
        }
    }
}
