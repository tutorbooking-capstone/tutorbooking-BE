using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.DTOs.RatingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                    Title = "PUSH_ON_TUTOR_RATING_RECEIVED",
                    Content = "PUSH_ON_TUTOR_RATING_RECEIVED",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
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

        public async Task<TutorRatingResponse> GetTutorRatingAsync(string tutorId)
        {
            var result = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var teachingQuality = await _unitOfWork.GetRepository<BookingSlotRating>().ExistEntities()
                .Where(b => b.TutorId.Equals(tutorId))
                .Select(e => e.TeachingQuality)
                .DefaultIfEmpty()
                .AverageAsync();

                var attitude = await _unitOfWork.GetRepository<BookingSlotRating>().ExistEntities()
                .Where(b => b.TutorId.Equals(tutorId))
                .Select(e => e.Attitude)
                .DefaultIfEmpty()
                .AverageAsync();

                var commitment = await _unitOfWork.GetRepository<BookingSlotRating>().ExistEntities()
                .Where(b => b.TutorId.Equals(tutorId))
                .Select(e => e.Commitment)
                .DefaultIfEmpty()
                .AverageAsync();
                return (teachingQuality, attitude, commitment);
            });

            return new TutorRatingResponse()
            {
                TutorId = tutorId,
                AverageTeachingQuality = result.teachingQuality,
                AverageAttitude = result.attitude,
                AverageCommitment = result.commitment
            };
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
