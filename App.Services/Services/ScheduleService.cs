using App.Services.Interfaces;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScheduleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Private Helpers
        private DateTime ConvertToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // Giả định rằng DateTime.Unspecified là local time (hoặc UTC tùy theo logic của bạn)
                // Ở đây, giả định là UTC để tránh lỗi
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
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

            // Lấy tất cả các mẫu lịch tuần của gia sư có AppliedFrom <= ngày kết thúc
            // Bao gồm cả các slot thời gian liên quan và sắp xếp giảm dần theo AppliedFrom
            var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId && p.AppliedFrom <= utcEndDate)
                .Include(p => p.Slots)
                .OrderByDescending(p => p.AppliedFrom)
                .ToListAsync();

            var result = new List<DailyAvailabilityDTO>();

            // Duyệt qua từng mẫu lịch tuần
            foreach (var pattern in patterns)
            {
                // Bắt đầu từ ngày áp dụng của mẫu lịch
                var currentWeekStart = ConvertToUtc(pattern.AppliedFrom);
                
                // Duyệt qua từng tuần cho đến khi vượt quá ngày kết thúc
                while (currentWeekStart <= utcEndDate)
                {
                    // Nếu cả tuần này nằm trước ngày bắt đầu thì bỏ qua
                    if (currentWeekStart.AddDays(6) < utcStartDate)
                    {
                        currentWeekStart = currentWeekStart.AddDays(7);
                        continue;
                    }

                    // Nhóm các slot theo ngày trong tuần
                    var groupedSlots = pattern.Slots?.GroupBy(s => s.DayInWeek) ?? Enumerable.Empty<IGrouping<DayInWeek, AvailabilitySlot>>();
                    
                    // Duyệt qua từng ngày trong tuần
                    foreach (var dailySlots in groupedSlots)
                    {
                        var dayInWeek = dailySlots.Key;
                        // Tính toán ngày cụ thể dựa trên ngày bắt đầu tuần và thứ trong tuần
                        var date = AvailabilitySlot.CalculateDateForDay(currentWeekStart, dayInWeek);

                        // Bỏ qua nếu ngày nằm ngoài khoảng yêu cầu
                        if (date < utcStartDate.Date || date > utcEndDate.Date)
                            continue;

                        // Thêm thông tin lịch vào kết quả
                        result.Add(new DailyAvailabilityDTO
                        {
                            Date = date,
                            Day = dayInWeek,
                            TimeSlots = dailySlots.Select(s => new TimeSlotDTO
                            {
                                SlotIndex = s.SlotIndex,
                                StartTime = TimeSpan.FromMinutes(s.SlotIndex * 30),  // Mỗi slot là 30 phút
                                EndTime = TimeSpan.FromMinutes((s.SlotIndex + 1) * 30),
                                Type = s.Type  // Loại slot (Available/Unavailable/Booked)
                            }).ToList()
                        });
                    }

                    // Di chuyển sang tuần tiếp theo
                    currentWeekStart = currentWeekStart.AddDays(7);
                }
            }

            // Trả về kết quả đã sắp xếp theo ngày
            return result.OrderBy(d => d.Date).ToList();
        }
    }
}