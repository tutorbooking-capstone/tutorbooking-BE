using App.Core.Base;
using App.DTOs.NotificationDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorBooking.APIService.EventHandlers;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly PushNotificationEventHandler _notificationEventHandler;

        public NotificationController(INotificationService notificationService, PushNotificationEventHandler notificationEventHandler)
        {
            _notificationService = notificationService;
            _notificationEventHandler = notificationEventHandler;
        }

        [HttpPost("send-to-roles")]
        [Authorize]
        public async Task<IActionResult> SendNotificationToRoles(SendNotificationToRolesRequest request)
        {
            var response = await _notificationService.SendToRolesAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "SUCCESS"
            ));
        }

        [HttpPost("send-to-users")]
        [Authorize]
        public async Task<IActionResult> SendNotificationToUsers(SendNotificationToUsersRequest request)
        {
            var response = await _notificationService.SendToUsersAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "SUCCESS"
            ));
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetNotificationsOfUser(int page = 1, int size = 10, bool isUnreadOnly = false)
        {
            var response = await _notificationService.GetNotificationsOfUserAsync(page, size, isUnreadOnly);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpGet("sender")]
        [Authorize]
        public async Task<IActionResult> GetSenderById(string userId)
        {
            var response = await _notificationService.GetSenderByIdAsync(userId);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }

        [HttpGet("sender/tutor")]
        [Authorize]
        public async Task<IActionResult> GetTutorSenderById(string userId)
        {
            var response = await _notificationService.GetTutorSenderByIdAsync(userId);
            return Ok(new BaseResponseModel<object>(
                data: response,
                message: "SUCCESS"
            ));
        }
    }
}
