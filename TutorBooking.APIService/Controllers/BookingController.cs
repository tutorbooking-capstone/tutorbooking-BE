using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorBooking.APIService.EventHandlers;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly PushNotificationEventHandler _notificationEventHandler;

        public BookingController(
            IBookingService bookingService, PushNotificationEventHandler notificationEventHandler)
        {
            _bookingService = bookingService;
            _notificationEventHandler = notificationEventHandler;
        }

        [HttpGet("learner")]
        public async Task<IActionResult> GetLearnerBookings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var bookings = await _bookingService.GetLearnerBookingsAsync(page, pageSize);
            var bookingStatusMetadata = EnumHelper.GetEnumMetadata(typeof(BookingStatus));
            
            return Ok(new BaseResponseModel<BasePaginatedList<BookingListItemDTO>>(
                data: bookings,
                additionalData: new { BookingStatus = bookingStatusMetadata },
                message: "Danh sách booking của học viên"
            ));
        }

        [HttpGet("tutor")]
        public async Task<IActionResult> GetTutorBookings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] BookingType bookingType = BookingType.All)  
        {
            var bookings = await _bookingService.GetTutorBookingsAsync(page, pageSize, bookingType);
            var bookingTypeMetadata = EnumHelper.GetEnumMetadata(typeof(BookingType));
            var bookingStatusMetadata = EnumHelper.GetEnumMetadata(typeof(BookingStatus));
            
            return Ok(new BaseResponseModel<BasePaginatedList<BookingListItemDTO>>(
                data: bookings,
                additionalData: new { 
                    BookingType = bookingTypeMetadata, 
                    BookingStatus = bookingStatusMetadata 
                },
                message: "Danh sách booking của giáo viên"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(string id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            return Ok(new BaseResponseModel<BookingDetailDTO>(
                data: booking,
                message: "Chi tiết booking"
            ));
        }

        [HttpGet("all")]
        [AuthorizeRoles(Role.Admin, Role.Staff)]
        public async Task<IActionResult> GetAllBookings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(page, pageSize);
            return Ok(new BaseResponseModel<BasePaginatedList<BookingListItemDTO>>(
                data: bookings,
                message: "Tất cả booking"
            ));
        }
    }
}