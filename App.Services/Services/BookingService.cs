using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public BookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<BasePaginatedList<BookingListItemDTO>> GetLearnerBookingsAsync(int page = 1, int pageSize = 10)
        {
            var learnerId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(learnerId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");

            var query = _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Where(b => b.LearnerId == learnerId)
                .Include(b => b.Tutor!).ThenInclude(t => t.User)
                .Include(b => b.Learner!).ThenInclude(l => l.User)
                .Include(b => b.LessonSnapshot)
                .Include(b => b.BookedSlots!).ThenInclude(bs => bs.HeldFund)
                .OrderByDescending(b => b.CreatedTime);

            return await GetPaginatedBookingsAsync(query, page, pageSize);
        }

        public async Task<BasePaginatedList<BookingListItemDTO>> GetTutorBookingsAsync(int page = 1, int pageSize = 10)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(tutorId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");

            var query = _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Where(b => b.TutorId == tutorId)
                .Include(b => b.Tutor!).ThenInclude(t => t.User)
                .Include(b => b.Learner!).ThenInclude(l => l.User)
                .Include(b => b.LessonSnapshot)
                .Include(b => b.BookedSlots!).ThenInclude(bs => bs.HeldFund)
                .OrderByDescending(b => b.CreatedTime);

            return await GetPaginatedBookingsAsync(query, page, pageSize);
        }

        public async Task<BookingDetailDTO> GetBookingDetailAsync(string bookingId)
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");

            var booking = await _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.Tutor!).ThenInclude(t => t.User)
                .Include(b => b.Learner!).ThenInclude(l => l.User)
                .Include(b => b.LessonSnapshot)
                .Include(b => b.BookedSlots!)
                .ThenInclude(bs => bs.HeldFund)
                .FirstOrDefaultAsync(b => b.Id == bookingId && (b.LearnerId == userId || b.TutorId == userId));

            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Booking not found or you don't have permission to view it.");

            return BookingDetailDTO.FromEntity(booking);
        }

        public async Task<BookingDetailDTO> GetBookingByIdAsync(string bookingId)
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");

            bool isAdminOrStaff = _currentUserProvider.IsInRole(Role.Admin.ToStringRole()) || 
                                    _currentUserProvider.IsInRole(Role.Staff.ToStringRole());

            var booking = await _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.Tutor!).ThenInclude(t => t.User)
                .Include(b => b.Learner!).ThenInclude(l => l.User)
                .Include(b => b.LessonSnapshot)
                .Include(b => b.BookedSlots!)
                .ThenInclude(bs => bs.HeldFund)
                .FirstOrDefaultAsync(b => b.Id == bookingId && 
                                        (isAdminOrStaff || b.LearnerId == userId || b.TutorId == userId));

            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Booking not found or you don't have permission to view it.");

            return BookingDetailDTO.FromEntity(booking);
        }

        public async Task<BasePaginatedList<BookingListItemDTO>> GetAllBookingsAsync(int page = 1, int pageSize = 10)
        {
            if (!_currentUserProvider.IsInRole(Role.Admin.ToStringRole()) && 
                !_currentUserProvider.IsInRole(Role.Staff.ToStringRole()))
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "You don't have permission to view all bookings.");

            var query = _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.Tutor!).ThenInclude(t => t.User)
                .Include(b => b.Learner!).ThenInclude(l => l.User)
                .Include(b => b.LessonSnapshot)
                .Include(b => b.BookedSlots!).ThenInclude(bs => bs.HeldFund)
                .OrderByDescending(b => b.CreatedTime);

            return await GetPaginatedBookingsAsync(query, page, pageSize);
        }

        #region Private Helpers
        private async Task<BasePaginatedList<BookingListItemDTO>> GetPaginatedBookingsAsync(
            IQueryable<Booking> query, int page, int pageSize)
        {
            var totalCount = await query.CountAsync();
            
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var bookingDTOs = bookings.Select(BookingListItemDTO.FromEntity).ToList();

            return new BasePaginatedList<BookingListItemDTO>(
                bookingDTOs, totalCount, page, pageSize);
        }
        #endregion
    }
}