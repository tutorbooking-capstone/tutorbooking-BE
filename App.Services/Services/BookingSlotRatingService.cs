using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.DTOs.RatingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

namespace App.Services.Services
{
    public class BookingSlotRatingService : IBookingSlotRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public BookingSlotRatingService(IUnitOfWork unitOfWork, IUserService userService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<BookingSlotRating> CreateAsync(BookingSlotRatingRequest request)
        {
            var booking = await _unitOfWork.GetRepository<Booking>().ExistEntities()
                .Include(b => b.BookedSlots)
                .FirstOrDefaultAsync(b => b.Id.Equals(request.BookingSlotId));
            BookingEligibleForCreate(booking);
            
            var entity = request.ToEntity(booking.TutorId, booking.LearnerId);
            _unitOfWork.GetRepository<BookingSlotRating>().Insert(entity);
            await _unitOfWork.SaveAsync();

            await _notificationService.SendToUsersAsync(new DTOs.NotificationDTOs.SendNotificationToUsersRequest()
            {
                Content = new()
                {
                    NotificationPriority = Repositories.Models.Notifications.ENotificationPriority.Normal,
                    Title = "Bạn đã nhận được đánh giá",
                    Content = $"Đánh giá trung bình: {(entity.Attitude + entity.TeachingQuality + entity.Commitment) / 3}",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "TutorReceivedRating",
                        Id = entity.Id,
                        BookingId = entity.BookingId,
                        AverageRating = (entity.Attitude + entity.TeachingQuality + entity.Commitment)/3,
                        SenderId = entity.LearnerId,
                    }),
                },
                ReceiverUserIds = [entity.TutorId]
            });

            return entity;
        }

        public async Task<TutorRatingResponse?> GetTutorRatingAsync(string tutorId, int page = 1, int size = 10)
        {
            return await _unitOfWork.GetRepository<BookingSlotRating>().ExistEntities()
                    .Where(b => b.TutorId.Equals(tutorId))
                    .GroupBy(b => b.TutorId)
                    .Select(g => new TutorRatingResponse()
                    {
                        TutorId = g.Key,
                        AverageTeachingQuality = g.Average(e => e.TeachingQuality),
                        AverageAttitude = g.Average(e => e.Attitude),
                        AverageCommitment = g.Average(e => e.Commitment),
                        Reviews = g.Select(e => new
                        {
                            Id = e.Id,
                            TeachingQuality = e.TeachingQuality,
                            Attitude= e.Attitude,
                            Commitment= e.Commitment,
                            Comment = e.Comment,
                            CreatedTime = e.CreatedTime,
                            LearnerName= e.Learner.User.FullName,
                            ProfilePictureUrl = e.Learner.User.ProfilePictureUrl,
                        }).Skip((page-1) * size)
                        .Take(size)
                        .ToArray()
                    })
                    .FirstOrDefaultAsync();
        }

        public async Task<BookingSlotRating> GetByIdAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<BookingSlotRating>().GetByIdAsync(id);
            if (entity == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "BOOKING_SLOT_RATING_NOT_FOUND");
            return entity;
        }

        public async Task<BookingSlotRating?> GetByBookingIdAsync(string bookingSlotId)
        {
            var entity = await _unitOfWork.GetRepository<BookingSlotRating>().ExistEntities()
                .FirstOrDefaultAsync(b => b.BookingId.Equals(bookingSlotId));
            return entity;
        }

        public async Task UpdateAsync(BookingSlotRatingUpdateRequest request)
        {
            var entity = await _unitOfWork.GetRepository<BookingSlotRating>().GetByIdAsync(request.Id);
            EntityEligibleForEdit(entity);

            request.UpdateEntity(ref entity);
            _unitOfWork.GetRepository<BookingSlotRating>().Update(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<BookingSlotRating>().GetByIdAsync(id);
            EntityEligibleForEdit(entity);
            _unitOfWork.GetRepository<BookingSlotRating>().Delete(entity);
            await _unitOfWork.SaveAsync();
        }

        #region Private Methods
        private void EntityEligibleForEdit(BookingSlotRating? entity)
        {
            if (entity == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "BOOKING_SLOT_RATING_NOT_FOUND");
            if (!entity.LearnerId.Equals(_userService.GetCurrentUserId()))
                throw new ErrorException((int)StatusCode.Forbidden, ErrorCode.Forbidden, "ENTITY_NOT_BELONG_TO_THE_LOGGED_IN_LEARNER");
            if (entity.CreatedTime.AddDays(7) < DateTime.UtcNow) 
                throw new ErrorException((int)StatusCode.Forbidden, ErrorCode.Forbidden, "EDIT_PERIOD_EXPIRED");
        }

        private void BookingEligibleForCreate(Booking? booking)
        {
            if (booking == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "BOOKING_NOT_FOUND");
            if (!booking.LearnerId.Equals(_userService.GetCurrentUserId()))
                throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "BOOKING_NOT_BELONG_TO_THE_LOGGED_IN_LEARNER");
            if (!booking.BookedSlots.Any(b => b.Status == SlotStatus.Completed))
                throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "REQUIRES_AT_LEAST_1_COMPLETED_SLOT");
        }
        #endregion
    }
}
