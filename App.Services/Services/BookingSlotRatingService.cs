using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Services
{
    public class BookingSlotRatingService : IBookingSlotRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public BookingSlotRatingService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<BookingSlotRating> CreateAsync(BookingSlotRatingRequest request)
        {
            var bookingSlot = await _unitOfWork.GetRepository<BookingSlot>().ExistEntities()
                .Include(b => b.BookedSlots)
                .FirstOrDefaultAsync(b => b.Id.Equals(request.BookingSlotId));
            BookingSlotEligibleForCreate(bookingSlot);
            
            var entity = request.ToEntity(bookingSlot.TutorId, bookingSlot.LearnerId);
            _unitOfWork.GetRepository<BookingSlotRating>().Insert(entity);
            await _unitOfWork.SaveAsync();
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

        private void BookingSlotEligibleForCreate(BookingSlot? bookingSlot)
        {
            if (bookingSlot == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "BOOKING_SLOT_NOT_FOUND");
            if (!bookingSlot.LearnerId.Equals(_userService.GetCurrentUserId()))
                throw new ErrorException((int)StatusCode.Forbidden, ErrorCode.Forbidden, "BOOKING_SLOT_NOT_BELONG_TO_THE_LOGGED_IN_LEARNER");
            if (!bookingSlot.BookedSlots.Any(b => b.Status == SlotStatus.Completed))
                throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "REQUIRES_AT_LEAST_1_COMPLETED_SLOT");
        }
        #endregion
    }
}
