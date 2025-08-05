using App.Core.Base;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.AccessControl;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.NotificationHubs;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly ConnectionService _connectionService;  

        public NotificationController(
            IHubContext<NotificationHub, INotificationClient> hubContext, 
            INotificationService notificationService,
            ConnectionService connectionService)  
        {
            _hubContext = hubContext;
            _notificationService = notificationService;
            _connectionService = connectionService; 
        }

        [HttpPost("send-to-roles")]
        [Authorize]
        public async Task<IActionResult> SendNotificationToRoles(SendNotificationToRolesRequest request)
        {
            var response = await _notificationService.CreateForRolesAsync(request);

            foreach(var role in request.Roles)
                await _hubContext.Clients.Group(role.ToStringRole()).ReceiveNotification(200, response);

            return Ok(new BaseResponseModel<string>(
                message: "SUCCESS"
            ));
        }

        [HttpPost("send-to-users")]
        [Authorize]
        public async Task<IActionResult> SendNotificationToUsers(SendNotificationToUsersRequest request)
        {
            var response = await _notificationService.CreateForUsersAsync(request);

            // Sử dụng extension method đã được cập nhật
            await _hubContext.SendNotificationToUsersAsync(_notificationService, _connectionService, request);
            
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
