using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public ScheduleService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<List<WeeklyAvailabilityPatternDTO>> GetTutorAvailabilityPatternsAsync(string tutorId)
        {
            var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId)
                .Include(p => p.Slots)
                .OrderByDescending(p => p.AppliedFrom)
                .ToListAsync();
                
            return patterns.Select(p => p.ToDTO()).ToList();
        }

        public async Task<List<BookingSlot>> GetTutorBookingSlotsAsync(string tutorId)
        {
            return await _unitOfWork.GetRepository<BookingSlot>()
                .ExistEntities()
                .Where(b => b.TutorId == tutorId)
                .Include(b => b.Slots)
                .ToListAsync();
        }

        public async Task<WeeklyAvailabilityPatternDTO> CreateWeeklyAvailabilityPatternAsync(WeeklyAvailabilityPatternDTO patternDto)
        {
            var tutorId = _userService.GetCurrentUserId();
            
            // 1. Validate the pattern against business rules
            await ValidatePatternAgainstBookingsAsync(tutorId, patternDto);
            
            // 2. Check for existing pattern with same AppliedFrom
            var existingPattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Include(p => p.Slots)
                .FirstOrDefaultAsync(p => p.TutorId == tutorId && p.AppliedFrom == patternDto.AppliedFrom);
            
            // 3. Handle existing pattern (delete if found)
            if (existingPattern != null)
            {
                _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().Delete(existingPattern);
            }
            
            // 4. Create and save new pattern
            var newPattern = patternDto.ToEntity(tutorId);
            _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().Insert(newPattern);
            await _unitOfWork.SaveAsync();
            
            return newPattern.ToDTO();
        }
        
        private async Task ValidatePatternAgainstBookingsAsync(string tutorId, WeeklyAvailabilityPatternDTO patternDto)
        {
            // 1. Get all future bookings for tutor from AppliedFrom date
            var bookings = await _unitOfWork.GetRepository<BookingSlot>()
                .ExistEntities()
                .Include(b => b.Slots)
                .Where(b => b.TutorId == tutorId)
                .ToListAsync();
            
            // 2. Create a set of booked slots (day, index) for easy lookup
            var bookedSlots = new HashSet<(DayInWeek Day, int SlotIndex)>();
            
            foreach (var booking in bookings)
            {
                // Skip bookings that end before AppliedFrom
                if (!IsBookingOverlappingWithPattern(booking, patternDto.AppliedFrom))
                    continue;
                    
                foreach (var slot in booking.Slots)
                {
                    bookedSlots.Add((slot.DayInWeek, slot.SlotIndex));
                }
            }
            
            // 3. Check if any unavailable slots in pattern conflict with booked slots
            foreach (var day in patternDto.DailyAvailabilities)
            {
                foreach (var slot in day.TimeSlots.Where(s => s.Type == SlotType.Unavailable))
                {
                    if (bookedSlots.Contains((day.Day, slot.SlotIndex)))
                    {
                        throw new InvalidOperationException(
                            $"Cannot mark slot as unavailable on {day.Day} at index {slot.SlotIndex} because it's already booked.");
                    }
                }
            }
        }
        
        private bool IsBookingOverlappingWithPattern(BookingSlot booking, DateTime patternStartDate)
        {
            // For one-time booking
            if (booking.RepeatForWeeks == null || booking.RepeatForWeeks == 0)
                return booking.StartDate >= patternStartDate;
                
            // For repeating booking, check if the last occurrence is after patternStartDate
            var lastOccurrence = booking.StartDate.AddDays((booking.RepeatForWeeks.Value) * 7);
            return lastOccurrence >= patternStartDate;
        }

        public async Task<bool> DeleteWeeklyAvailabilityPatternAsync(string patternId)
        {
            var tutorId = _userService.GetCurrentUserId();
            
            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .FirstOrDefaultAsync(p => p.Id == patternId && p.TutorId == tutorId);
                
            if (pattern == null)
                return false;
                
            // Only allow deletion of future patterns
            if (pattern.AppliedFrom <= DateTime.Today)
                throw new InvalidOperationException("Cannot delete patterns from the past or current date.");
                
            _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().Delete(pattern);
            await _unitOfWork.SaveAsync();
            
            return true;
        }
        
        public async Task<List<DailyAvailabilityDTO>> GetAvailabilityForWeekAsync(string tutorId, DateTime weekStartDate)
        {
            // Adjust weekStartDate to Monday if not already
            while (weekStartDate.DayOfWeek != DayOfWeek.Monday)
                weekStartDate = weekStartDate.AddDays(-1);
                
            // Get the most recent pattern applicable to this week
            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Include(p => p.Slots)
                .Where(p => p.TutorId == tutorId && p.AppliedFrom <= weekStartDate)
                .OrderByDescending(p => p.AppliedFrom)
                .FirstOrDefaultAsync();
                
            if (pattern == null)
                return new List<DailyAvailabilityDTO>();
                
            // Get bookings that apply to this week
            var weekEndDate = weekStartDate.AddDays(6);
            var bookings = await _unitOfWork.GetRepository<BookingSlot>()
                .ExistEntities()
                .Include(b => b.Slots)
                .Where(b => b.TutorId == tutorId)
                .ToListAsync();
                
            var applicableBookings = bookings.Where(b => 
                IsBookingApplicableToWeek(b, weekStartDate, weekEndDate)).ToList();
                
            // Create merged availability that reflects both pattern and bookings
            var result = CreateMergedAvailability(pattern, applicableBookings, weekStartDate);
            
            return result;
        }
        
        private bool IsBookingApplicableToWeek(BookingSlot booking, DateTime weekStartDate, DateTime weekEndDate)
        {
            // Logic to determine if booking applies to the given week
            // ...
            return true; // Simplified for now
        }
        
        private List<DailyAvailabilityDTO> CreateMergedAvailability(
            WeeklyAvailabilityPattern pattern, 
            List<BookingSlot> bookings,
            DateTime weekStartDate)
        {
            // Logic to merge pattern and bookings into daily availability
            // ...
            return new List<DailyAvailabilityDTO>(); // Simplified for now
        }

        Task<List<DTOs.AppUserDTOs.TutorDTOs.BookingSlotDTO>> IScheduleService.GetTutorBookingSlotsAsync(string tutorId)
        {
            throw new NotImplementedException();
        }

    }
}