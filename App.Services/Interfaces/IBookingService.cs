using App.Core.Base;
using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface IBookingService
    {
        // Lấy danh sách booking của learner (người học)
        Task<BasePaginatedList<BookingListItemDTO>> GetLearnerBookingsAsync(int page = 1, int pageSize = 10);
        
        // Lấy danh sách booking của tutor (người dạy)
        Task<BasePaginatedList<BookingListItemDTO>> GetTutorBookingsAsync(int page = 1, int pageSize = 10);
        
        // Lấy chi tiết booking bao gồm các booked slots và held funds
        Task<BookingDetailDTO> GetBookingDetailAsync(string bookingId);
        
        // Lấy booking theo ID
        Task<BookingDetailDTO> GetBookingByIdAsync(string bookingId);
        
        // Admin/Staff có thể xem tất cả booking
        Task<BasePaginatedList<BookingListItemDTO>> GetAllBookingsAsync(int page = 1, int pageSize = 10);
    }
}