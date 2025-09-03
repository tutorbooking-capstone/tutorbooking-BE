using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
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

        [HttpGet("metadata")]
        [AllowAnonymous]
        public IActionResult GetBookingMetadata()
        {
            var bookingStatusMetadata = EnumHelper.GetEnumMetadata(typeof(BookingStatus));
            var slotStatusMetadata = EnumHelper.GetEnumMetadata(typeof(SlotStatus));
            var heldFundStatusMetadata = EnumHelper.GetEnumMetadata(typeof(HeldFundStatus));
            var heldFundTypeMetadata = EnumHelper.GetEnumMetadata(typeof(HeldFundType));
            var bookingTypeMetadata = EnumHelper.GetEnumMetadata(typeof(BookingType));
            
            return Ok(new BaseResponseModel<object>(
                data: new
                {
                    BookingStatus = bookingStatusMetadata,
                    SlotStatus = slotStatusMetadata,
                    HeldFundStatus = heldFundStatusMetadata,
                    HeldFundType = heldFundTypeMetadata,
                    BookingType = bookingTypeMetadata
                },
                message: "Booking metadata"
            ));
        }

        [HttpGet("metadata/booking-status")]
        [AllowAnonymous]
        public IActionResult GetBookingStatusMetadata()
        {
            var metadata = EnumHelper.GetEnumMetadata(typeof(BookingStatus));
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Booking status metadata"
            ));
        }

        [HttpGet("metadata/slot-status")]
        [AllowAnonymous]
        public IActionResult GetSlotStatusMetadata()
        {
            var metadata = EnumHelper.GetEnumMetadata(typeof(SlotStatus));
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Slot status metadata"
            ));
        }

        [HttpGet("metadata/held-fund-status")]
        [AllowAnonymous]
        public IActionResult GetHeldFundStatusMetadata()
        {
            var metadata = EnumHelper.GetEnumMetadata(typeof(HeldFundStatus));
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Held fund status metadata"
            ));
        }

        [HttpGet("metadata/held-fund-type")]
        [AllowAnonymous]
        public IActionResult GetHeldFundTypeMetadata()
        {
            var metadata = EnumHelper.GetEnumMetadata(typeof(HeldFundType));
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Held fund type metadata"
            ));
        }

        [HttpGet("metadata/booking-type")]
        [AllowAnonymous]
        public IActionResult GetBookingTypeMetadata()
        {
            var metadata = EnumHelper.GetEnumMetadata(typeof(BookingType));
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Booking type metadata"
            ));
        }
    }
}