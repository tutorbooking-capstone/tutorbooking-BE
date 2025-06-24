using App.Services.Interfaces;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.DTOs.ScheduleDTOs;
using App.Core.Provider;
using App.Core.Base;
using Microsoft.AspNetCore.Http;
using App.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public ScheduleService(IUnitOfWork unitOfWork, ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

        #region Private Helpers
        private DateTime ConvertToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                
            return dateTime.ToUniversalTime();
        }
        #endregion
        public async Task<List<DailyAvailabilityDTO>> GetTutorAvailabilityAsync(string tutorId, DateTime startDate, DateTime endDate)
        {
            // Kiểm tra ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc
            if (startDate > endDate)
                throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");

            // Chuyển đổi ngày giờ về UTC để đồng bộ
            var utcStartDate = ConvertToUtc(startDate);
            var utcEndDate = ConvertToUtc(endDate);

            // Get patterns without including slots
            var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId && p.AppliedFrom <= utcEndDate)
                .OrderByDescending(p => p.AppliedFrom)
                .ToListAsync();

            // Get pattern IDs
            var patternIds = patterns.Select(p => p.Id).ToList();

            // Get slots separately
            var slots = await _unitOfWork.GetRepository<AvailabilitySlot>()
                .ExistEntities()
                .Where(s => s.WeeklyPatternId != null && patternIds.Contains(s.WeeklyPatternId))
                .ToListAsync();

            // Get booked slots in the date range
            var bookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Where(bs => bs.BookedDate >= utcStartDate && bs.BookedDate <= utcEndDate)
                .Include(bs => bs.BookingSlot) // Get booking details
                .ToListAsync();

            var result = new List<DailyAvailabilityDTO>();

            // Process each pattern
            foreach (var pattern in patterns)
            {
                var currentWeekStart = ConvertToUtc(pattern.AppliedFrom);
                
                while (currentWeekStart <= utcEndDate)
                {
                    if (currentWeekStart.AddDays(6) < utcStartDate)
                    {
                        currentWeekStart = currentWeekStart.AddDays(7);
                        continue;
                    }

                    // Get slots for this pattern
                    var patternSlots = slots.Where(s => s.WeeklyPatternId == pattern.Id).ToList();
                    var groupedSlots = patternSlots.GroupBy(s => s.DayInWeek);
                    
                    foreach (var dailySlots in groupedSlots)
                    {
                        var dayInWeek = dailySlots.Key;
                        var date = AvailabilitySlot.CalculateDateForDay(currentWeekStart, dayInWeek);

                        if (date < utcStartDate.Date || date > utcEndDate.Date)
                            continue;

                        var timeSlots = new List<TimeSlotDTO>();
                        
                        foreach (var slot in dailySlots)
                        {
                            // Check if slot is booked on this date
                            var booking = bookedSlots.FirstOrDefault(bs => 
                                bs.AvailabilitySlotId == slot.Id && 
                                bs.BookedDate.Date == date.Date);
                            
                            timeSlots.Add(new TimeSlotDTO
                            {
                                SlotIndex = slot.SlotIndex,
                                StartTime = TimeSpan.FromMinutes(slot.SlotIndex * 30),
                                EndTime = TimeSpan.FromMinutes((slot.SlotIndex + 1) * 30),
                                Type = booking != null ? SlotType.Booked : slot.Type,
                                BookingId = booking?.BookingSlotId,
                                LearnerId = booking?.BookingSlot?.LearnerId,
                                Note = booking?.BookingSlot?.Note
                            });
                        }

                        result.Add(new DailyAvailabilityDTO
                        {
                            Date = date,
                            Day = dayInWeek,
                            TimeSlots = timeSlots
                        });
                    }

                    currentWeekStart = currentWeekStart.AddDays(7);
                }
            }

            return result.OrderBy(d => d.Date).ToList();
        }
    
        public async Task<WeeklyPatternResponse> UpdateWeeklyPatternAsync(UpdateWeeklyPatternRequest request)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "User is not authenticated.");

            var appliedFromDate = request.AppliedFrom.Date;

            // Business Rule 1 & 4: AppliedFrom phải lớn hơn ngày hiện tại
            var today = DateTime.UtcNow.Date;
            if (appliedFromDate <= today)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Availability can only be set for future dates, starting from tomorrow.");

            // Business Rule 2: AppliedFrom phải là Thứ Hai
            if (appliedFromDate.DayOfWeek != DayOfWeek.Monday)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "The start date for a weekly pattern must be a Monday.");

            var patternRepo = _unitOfWork.GetRepository<WeeklyAvailabilityPattern>();

            // Business Rule 3: Tìm và xóa pattern cũ nếu có cùng AppliedFrom
            var existingPattern = await patternRepo.ExistEntities()
                .FirstOrDefaultAsync(p => p.TutorId == tutorId && p.AppliedFrom == appliedFromDate);

            if (existingPattern != null)
                patternRepo.Delete(existingPattern);

            var newPattern = new WeeklyAvailabilityPattern
            {
                TutorId = tutorId,
                AppliedFrom = appliedFromDate,
                Slots = request.Slots.Select(s => new AvailabilitySlot
                {
                    Type = s.Type,
                    DayInWeek = s.DayInWeek,
                    SlotIndex = s.SlotIndex
                }).ToList()
            };

            patternRepo.Insert(newPattern);
            await _unitOfWork.SaveAsync();

            return await patternRepo.ExistEntities()
                .AsNoTracking()
                .Where(p => p.Id == newPattern.Id)
                .Select(WeeklyPatternResponse.Projection)
                .FirstAsync();
        }
    }
}