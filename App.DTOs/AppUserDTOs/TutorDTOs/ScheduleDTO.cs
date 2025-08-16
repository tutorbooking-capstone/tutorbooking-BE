using App.Repositories.Models;
using App.Repositories.Models.Scheduling;

namespace App.DTOs.ScheduleDTOs
{
    public class CreateWeeklyPatternRequest
    {
        public DateTime AppliedFrom { get; set; }
        public List<SlotRequest> Slots { get; set; } = new List<SlotRequest>();
    }

    public class SlotRequest
    {
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
    }

    public class WeeklyPatternDetailResponse
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();

        public static WeeklyPatternDetailResponse FromEntity(WeeklyAvailabilityPattern pattern)
        {
            return new WeeklyPatternDetailResponse
            {
                Id = pattern.Id,
                AppliedFrom = pattern.AppliedFrom,
                Slots = pattern.Slots?.Select(s => new AvailabilitySlotDTO
                {
                    DayInWeek = s.DayInWeek,
                    SlotIndex = s.SlotIndex
                }).ToList() ?? new List<AvailabilitySlotDTO>()
            };
        }
    }

    public class WeeklyPatternWithDatesResponse
    {
        public string Id { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
        public DateTime AppliedFrom { get; set; }

        public static WeeklyPatternWithDatesResponse FromEntity(WeeklyAvailabilityPattern pattern, DateTime? endDate = null)
        {
            return new WeeklyPatternWithDatesResponse
            {
                Id = pattern.Id,
                AppliedFrom = pattern.AppliedFrom,
                EndDate = endDate
            };
        }
    }

    public class TimeSlotResponse
    {
        public int SlotIndex { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public SlotType Type { get; set; } = SlotType.Available;

        public static TimeSlotResponse FromAvailabilitySlot(
            AvailabilitySlot slot, 
            BookedSlot? bookedSlot = null, 
            OfferedSlot? offeredSlot = null)
        {
            var status = bookedSlot != null 
                ? SlotType.Booked 
                : offeredSlot != null 
                    ? SlotType.OnHold 
                    : SlotType.Available;

            return new TimeSlotResponse
            {
                SlotIndex = slot.SlotIndex,
                StartTime = slot.GetStartTime(),
                EndTime = slot.GetEndTime(),
                Type = status
            };
        }
    }

    public class DailyScheduleResponse
    {
        public DayInWeek Day { get; init; }
        public DateTime Date { get; init; }
        public List<TimeSlotResponse> TimeSlots { get; init; } = new();

        public static DailyScheduleResponse Create(DayInWeek day, DateTime date, List<TimeSlotResponse> timeSlots)
        {
            return new DailyScheduleResponse
            {
                Day = day,
                Date = date,
                TimeSlots = timeSlots
            };
        }
    }

    public static class ScheduleMappingExtensions
    {
        public static List<WeeklyPatternWithDatesResponse> ToWeeklyPatternsWithDates(
            this List<WeeklyAvailabilityPattern> patterns)
        {
            var sortedPatterns = patterns.OrderByDescending(p => p.AppliedFrom).ToList();
            var result = new List<WeeklyPatternWithDatesResponse>();
            
            for (int i = 0; i < sortedPatterns.Count; i++)
            {
                var pattern = sortedPatterns[i];
                DateTime? endDate = null;
                
                if (i > 0)
                {
                    var newerPattern = sortedPatterns[i - 1];
                    endDate = newerPattern.AppliedFrom.AddDays(-1);
                }
                
                result.Add(WeeklyPatternWithDatesResponse.FromEntity(pattern, endDate));
            }
            
            return result;
        }

        public static List<TimeSlotResponse> ToTimeSlotResponses(
            this IEnumerable<AvailabilitySlot> slots,
            IEnumerable<BookedSlot> bookedSlots,
            IEnumerable<OfferedSlot> offeredSlots,
            DateTime date)
        {
            return slots.Select(slot => 
            {
                var bookedSlot = bookedSlots.FirstOrDefault(bs => 
                    bs.BookedDate.Date == date.Date && 
                    bs.SlotIndex == slot.SlotIndex);
                    
                var offeredSlot = offeredSlots.FirstOrDefault(os => 
                    os.SlotDateTime.Date == date.Date && 
                    os.SlotIndex == slot.SlotIndex);
                    
                return TimeSlotResponse.FromAvailabilitySlot(slot, bookedSlot, offeredSlot);
            }).ToList();
        }
    }
}