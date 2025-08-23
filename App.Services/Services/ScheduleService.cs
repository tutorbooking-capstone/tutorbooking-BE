using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.ScheduleDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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
                .Include(bs => bs.Booking) // Get booking details
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
                                //bs.AvailabilitySlotId == slot.Id && 
                                bs.BookedDate.Date == date.Date);
                            
                            timeSlots.Add(new TimeSlotDTO
                            {
                                SlotIndex = slot.SlotIndex,
                                StartTime = TimeSpan.FromMinutes(slot.SlotIndex * 30),
                                EndTime = TimeSpan.FromMinutes((slot.SlotIndex + 1) * 30),
                                //Type = booking != null ? SlotType.Booked : slot.Type,
                                BookingId = booking?.BookingId,
                                LearnerId = booking?.Booking?.LearnerId,
                                Note = booking?.Booking?.Note
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

            // Chuyển đổi DateTime sang UTC trước khi sử dụng
            var appliedFromDate = ConvertToUtc(request.AppliedFrom).Date;
            var today = DateTime.UtcNow.Date;

            // Business Rule 1 & 4: AppliedFrom phải lớn hơn ngày hiện tại
            if (appliedFromDate <= today)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Chỉ có thể đặt lịch rảnh cho các ngày trong tương lai, bắt đầu từ ngày mai.");

            // Business Rule 2: AppliedFrom phải là Thứ Hai
            if (appliedFromDate.DayOfWeek != DayOfWeek.Monday)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Ngày bắt đầu của lịch tuần phải là Thứ Hai.");

            // Validation đã được xử lý bởi FluentValidation, không cần check thủ công ở đây.

            var patternRepo = _unitOfWork.GetRepository<WeeklyAvailabilityPattern>();

            // Business Rule 3: Tìm và xóa pattern cũ nếu có cùng AppliedFrom
            var existingPattern = await patternRepo.ExistEntities()
                .FirstOrDefaultAsync(p => p.TutorId == tutorId && p.AppliedFrom == appliedFromDate);

            if (existingPattern != null)
                patternRepo.Delete(existingPattern);

            var availabilitySlots = request.Slots.Select(s => 
                AvailabilitySlot.Create(s.DayInWeek, s.SlotIndex)
            );

            var newPattern = WeeklyAvailabilityPattern.Create(tutorId, appliedFromDate, availabilitySlots);

            patternRepo.Insert(newPattern);
            await _unitOfWork.SaveAsync();

            // Trả về response bằng cách query lại từ DB để có ID và dữ liệu nhất quán
            return await patternRepo.ExistEntities()
                .AsNoTracking()
                .Where(p => p.Id == newPattern.Id)
                .Select(WeeklyPatternResponse.Projection)
                .FirstAsync();
        }

        public async Task<List<WeeklyPatternResponse>> GetAllWeeklyPatternsAsync(string tutorId)
        {
            // Kiểm tra tutor có tồn tại không
            var tutorExists = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy gia sư với ID đã cung cấp.");

            // Sử dụng Projection để ánh xạ trực tiếp trong câu query, giúp tối ưu hiệu suất
            return await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .AsNoTracking()
                .Where(p => p.TutorId == tutorId)
                .OrderByDescending(p => p.AppliedFrom)
                .Select(WeeklyPatternResponse.Projection)
                .ToListAsync();
        }

        public async Task<List<DailyAvailabilityPatternDTO>> GetWeekAvailabilityAsync(string tutorId, DateTime startDate)
        {
            // Kiểm tra tutor có tồn tại không
            var tutorExists = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy gia sư với ID đã cung cấp.");

            var normalizedStartDate = ConvertToUtc(startDate).Date;
            var endDate = normalizedStartDate.AddDays(6);

            var relevantPatterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId && p.AppliedFrom <= endDate)
                .OrderByDescending(p => p.AppliedFrom)
                .Include(p => p.Slots)
                .ToListAsync();

            // Nếu không có mẫu nào, trả về danh sách rỗng
            if (!relevantPatterns.Any())
                return new List<DailyAvailabilityPatternDTO>();

            var result = new List<DailyAvailabilityPatternDTO>();

            // Lặp qua từng ngày trong khoảng thời gian 7 ngày
            for (int i = 0; i < 7; i++)
            {
                var currentDate = normalizedStartDate.AddDays(i);
                DayInWeek currentDayOfWeek = (DayInWeek)((int)currentDate.DayOfWeek + 1);

                // Sau sắp xếp, mẫu đầu tiên có AppliedFrom <= currentDate là mẫu đúng
                var applicablePattern = relevantPatterns.FirstOrDefault(p => p.AppliedFrom <= currentDate);
                var timeSlotIndices = new List<int>();

                if (applicablePattern?.Slots != null)
                    timeSlotIndices = applicablePattern.Slots
                        .Where(s => s.DayInWeek == currentDayOfWeek)
                        .Select(s => s.SlotIndex)
                        .ToList();
                
                result.Add(DailyAvailabilityPatternDTO.Create(currentDayOfWeek, currentDate, timeSlotIndices));
            }

            return result;
        }

        public async Task<WeeklyPatternResponse> CreateWeeklyPatternAsync(CreateWeeklyPatternRequest request)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "User is not authenticated.");

            // Convert DateTime to UTC before using
            var appliedFromDate = ConvertToUtc(request.AppliedFrom).Date;
            var today = DateTime.UtcNow.Date;

            // Business Rule 1: AppliedFrom must be greater than today
            if (appliedFromDate <= today)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Chỉ có thể đặt lịch rảnh cho các ngày trong tương lai, bắt đầu từ ngày mai.");

            // Business Rule 2: AppliedFrom must be Monday
            if (appliedFromDate.DayOfWeek != DayOfWeek.Monday)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Ngày bắt đầu của lịch tuần phải là Thứ Hai.");

            // Business Rule 3: AppliedFrom must be unique for this tutor
            var existingPattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .AnyAsync(p => p.TutorId == tutorId && p.AppliedFrom == appliedFromDate);
            
            if (existingPattern)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Đã tồn tại lịch tuần bắt đầu từ ngày {appliedFromDate.ToString("dd/MM/yyyy")}.");

            var availabilitySlots = request.Slots.Select(s => 
                AvailabilitySlot.Create(s.DayInWeek, s.SlotIndex)
            );

            var newPattern = WeeklyAvailabilityPattern.Create(
                tutorId, 
                appliedFromDate, 
                availabilitySlots);

            _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().Insert(newPattern);
            await _unitOfWork.SaveAsync();

            // Return response by querying back from DB to have ID and consistent data
            return await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .AsNoTracking()
                .Where(p => p.Id == newPattern.Id)
                .Select(WeeklyPatternResponse.Projection)
                .FirstAsync();
        }

        public async Task<WeeklyPatternDetailResponse> GetWeeklyPatternDetailAsync(string patternId)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "User is not authenticated.");

            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Include(p => p.Slots)
                .FirstOrDefaultAsync(p => p.Id == patternId);

            if (pattern == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Weekly pattern not found.");

            if (pattern.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "You are not authorized to view this pattern.");

            return WeeklyPatternDetailResponse.FromEntity(pattern);
        }

        public async Task<List<WeeklyPatternWithDatesResponse>> GetWeeklyPatternsWithDatesAsync(string tutorId)
        {
            var tutorExists = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy gia sư với ID đã cung cấp.");

            // Get all patterns for this tutor, ordered by AppliedFrom date
            var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .AsNoTracking()
                .Where(p => p.TutorId == tutorId)
                .OrderByDescending(p => p.AppliedFrom)
                .ToListAsync();

            return patterns.ToWeeklyPatternsWithDates();
        }
        public async Task<List<DailyScheduleResponse>> GetTutorScheduleAsync(string tutorId, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");

            var utcStartDate = ConvertToUtc(startDate).Date;
            var utcEndDate = ConvertToUtc(endDate).Date;
            var utcEndDateInclusive = utcEndDate.AddDays(1);  

            var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId && p.AppliedFrom <= utcEndDate)
                .OrderBy(p => p.AppliedFrom) // Order by ascending to process them in chronological order
                .ToListAsync();

            if (!patterns.Any())
                return new List<DailyScheduleResponse>();

            var patternIds = patterns.Select(p => p.Id).ToList();

            // Get all availability slots for these patterns
            var availabilitySlots = await _unitOfWork.GetRepository<AvailabilitySlot>()
                .ExistEntities()
                .Where(s => s.WeeklyPatternId != null && patternIds.Contains(s.WeeklyPatternId))
                .ToListAsync();

            // Get all pending booked slots in date range
            var bookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Where(bs => 
                    bs.Booking!.TutorId == tutorId && 
                    bs.BookedDate >= utcStartDate && 
                    bs.BookedDate < utcEndDateInclusive &&  
                    bs.Status == SlotStatus.Pending)
                .Include(bs => bs.Booking)  
                .ToListAsync();

            // Get all offered slots in date range
            var offeredSlots = await _unitOfWork.GetRepository<OfferedSlot>()
                .ExistEntities()
                .Where(os => 
                    os.SlotDateTime >= utcStartDate && 
                    os.SlotDateTime < utcEndDateInclusive)  
                .ToListAsync();

            var result = new List<DailyScheduleResponse>();
            var patternEffectiveDates = patterns.GetPatternEffectiveDateRanges();

            for (DateTime currentDate = utcStartDate; currentDate <= utcEndDate; currentDate = currentDate.AddDays(1))
            {
                var effectivePattern = patterns.FindEffectivePatternForDate(patternEffectiveDates, currentDate);
                if (effectivePattern == null)
                    continue;
                    
                DayInWeek currentDayOfWeek = (DayInWeek)((int)currentDate.DayOfWeek + 1);
                var daySlots = availabilitySlots
                    .Where(s => s.WeeklyPatternId == effectivePattern.Id && s.DayInWeek == currentDayOfWeek)
                    .ToList();
                    
                if (!daySlots.Any())
                    continue;
                    
                var timeSlots = daySlots.ToTimeSlotResponses(
                    bookedSlots, 
                    offeredSlots, 
                    currentDate);
                    
                result.Add(DailyScheduleResponse.Create(
                    currentDayOfWeek,
                    currentDate,
                    timeSlots
                ));
            }
            
            return result.OrderBy(d => d.Date).ToList();
        }

        public async Task<List<SlotRequest>> GetBlockedSlotsForPatternAsync(string patternId)
        {
            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .FirstOrDefaultAsync(p => p.Id == patternId);
            
            if (pattern == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Weekly pattern not found.");
            
            // Find the next pattern (with later AppliedFrom)
            var nextPattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == pattern.TutorId && p.AppliedFrom > pattern.AppliedFrom)
                .OrderBy(p => p.AppliedFrom)
                .FirstOrDefaultAsync();
            
            // Determine the end date for this pattern
            var endDate = nextPattern?.AppliedFrom ?? DateTime.MaxValue;
            
            // Get all pending booked slots in the date range
            var bookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Where(bs => 
                    bs.Booking!.TutorId == pattern.TutorId && 
                    bs.BookedDate >= pattern.AppliedFrom && 
                    bs.BookedDate < endDate &&
                    bs.Status == SlotStatus.Pending)
                .ToListAsync();
            
            // Get all offered slots in the date range
            var offeredSlots = await _unitOfWork.GetRepository<OfferedSlot>()
                .ExistEntities()
                .Where(os => 
                    os.SlotDateTime >= pattern.AppliedFrom && 
                    os.SlotDateTime < endDate)
                .ToListAsync();
            
            // Create a list of blocked slots
            var blockedSlots = new List<SlotRequest>();
            
            // Process booked slots
            foreach (var bookedSlot in bookedSlots)
            {
                // Convert date to day of week
                var bookedDate = bookedSlot.BookedDate.Date;
                var dayOfWeek = (DayInWeek)((int)bookedDate.DayOfWeek + 1);
                
                blockedSlots.Add(new SlotRequest 
                { 
                    DayInWeek = dayOfWeek, 
                    SlotIndex = bookedSlot.SlotIndex 
                });
            }
            
            // Process offered slots
            foreach (var offeredSlot in offeredSlots)
            {
                // Convert date to day of week
                var offeredDate = offeredSlot.SlotDateTime.Date;
                var dayOfWeek = (DayInWeek)((int)offeredDate.DayOfWeek + 1);
                
                blockedSlots.Add(new SlotRequest 
                { 
                    DayInWeek = dayOfWeek, 
                    SlotIndex = offeredSlot.SlotIndex 
                });
            }
            
            // Return distinct blocked slots
            return blockedSlots
                .GroupBy(s => new { s.DayInWeek, s.SlotIndex })
                .Select(g => g.First())
                .ToList();
        }

        public async Task<WeeklyPatternResponse> EditWeeklyPatternAsync(string patternId, List<SlotRequest> newSlots)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "User is not authenticated.");

            // Get existing pattern
            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Include(p => p.Slots)
                .FirstOrDefaultAsync(p => p.Id == patternId);

            if (pattern == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Weekly pattern not found.");

            if (pattern.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "You are not authorized to edit this pattern.");

            // Get blocked slots
            var blockedSlots = await GetBlockedSlotsForPatternAsync(patternId);
            
            // Check if any of the current slots that will be removed are blocked
            var existingSlots = pattern.Slots!.Select(s => new SlotRequest { DayInWeek = s.DayInWeek, SlotIndex = s.SlotIndex }).ToList();
            var slotsToRemove = existingSlots.Except(newSlots, new SlotRequestComparer()).ToList();
            
            var blockedSlotsToRemove = slotsToRemove.Where(slot => 
                blockedSlots.Any(blockedSlot => 
                    blockedSlot.DayInWeek == slot.DayInWeek && blockedSlot.SlotIndex == slot.SlotIndex));
            
            if (blockedSlotsToRemove.Any())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Cannot remove slots that have pending bookings or offers.");
            
            // Remove all existing slots
            var slotRepo = _unitOfWork.GetRepository<AvailabilitySlot>();
            foreach (var slot in pattern.Slots!)
            {
                slotRepo.Delete(slot);
            }
            
            // Add new slots
            var availabilitySlots = newSlots.Select(s => AvailabilitySlot.Create(s.DayInWeek, s.SlotIndex));
            foreach (var slot in availabilitySlots)
            {
                slot.WeeklyPatternId = patternId;
                slotRepo.Insert(slot);
            }
            
            await _unitOfWork.SaveAsync();
            
            // Return response by querying back from DB
            return await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .AsNoTracking()
                .Where(p => p.Id == patternId)
                .Select(WeeklyPatternResponse.Projection)
                .FirstAsync();
        }

        public async Task<bool> CanDeleteWeeklyPatternAsync(string patternId)
        {
            var blockedSlots = await GetBlockedSlotsForPatternAsync(patternId);
            return !blockedSlots.Any();
        }

        public async Task DeleteWeeklyPatternAsync(string patternId)
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "User is not authenticated.");

            var patternRepo = _unitOfWork.GetRepository<WeeklyAvailabilityPattern>();
            var patternToDelete = await patternRepo
                .ExistEntities()
                .FirstOrDefaultAsync(p => p.Id == patternId);

            if (patternToDelete == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Weekly pattern not found.");

            if (patternToDelete.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "You are not authorized to delete this pattern.");

            // Business Rule: Chỉ cho phép xóa các pattern có ngày bắt đầu trong tương lai.
            var today = DateTime.UtcNow.Date;
            if (patternToDelete.AppliedFrom <= today)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể xóa các mẫu lịch tuần đã qua hoặc hiện tại.");

            // Kiểm tra xem pattern có chứa slot bị block không
            var blockedSlots = await GetBlockedSlotsForPatternAsync(patternId);
            if (blockedSlots.Any())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể xóa lịch tuần có các slot đã được đặt lịch hoặc đang được đề xuất.");

            patternRepo.Delete(patternToDelete);
            await _unitOfWork.SaveAsync();
        }

        public Task<Dictionary<string, object>> GetScheduleMetadataAsync()
        {
            // Lấy metadata từ các enum
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(SlotType),
                typeof(DayInWeek)
            );

            var convertedMetadata = enumMetadata.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );
            
            return Task.FromResult(convertedMetadata);
        }

        #region Private Helper
        private class SlotRequestComparer : IEqualityComparer<SlotRequest>
        {
            public bool Equals(SlotRequest? x, SlotRequest? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.DayInWeek == y.DayInWeek && x.SlotIndex == y.SlotIndex;
            }

            public int GetHashCode(SlotRequest obj)
            {
                return HashCode.Combine(obj.DayInWeek, obj.SlotIndex);
            }
        }
        #endregion
    }
}