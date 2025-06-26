using App.Services.Interfaces;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.DTOs.ScheduleDTOs;
using App.Core.Provider;
using App.Core.Base;
using Microsoft.AspNetCore.Http;
using App.Core.Constants;
using App.Repositories.Models.User;

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

            // Validate slots
            foreach (var slot in request.Slots)
            {
                if (!slot.IsValid())
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        "Một hoặc nhiều khung giờ rảnh không hợp lệ.");
            }

            var patternRepo = _unitOfWork.GetRepository<WeeklyAvailabilityPattern>();

            // Business Rule 3: Tìm và xóa pattern cũ nếu có cùng AppliedFrom
            var existingPattern = await patternRepo.ExistEntities()
                .FirstOrDefaultAsync(p => p.TutorId == tutorId && p.AppliedFrom == appliedFromDate);

            if (existingPattern != null)
                patternRepo.Delete(existingPattern);

            var availabilitySlots = AvailabilitySlotDTO.ToEntities(request.Slots);
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

            patternRepo.Delete(patternToDelete);
            await _unitOfWork.SaveAsync();
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
                        .Where(s => s.DayInWeek == currentDayOfWeek && s.Type == SlotType.Available)
                        .Select(s => s.SlotIndex)
                        .ToList();
                
                result.Add(DailyAvailabilityPatternDTO.Create(currentDayOfWeek, currentDate, timeSlotIndices));
            }

            return result;
        }
    }
}