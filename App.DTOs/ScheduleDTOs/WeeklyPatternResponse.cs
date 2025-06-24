using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

namespace App.DTOs.ScheduleDTOs
{
    public class WeeklyPatternResponse
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();

        public static Expression<Func<WeeklyAvailabilityPattern, WeeklyPatternResponse>> Projection =>
            pattern => new WeeklyPatternResponse
            {
                Id = pattern.Id,
                AppliedFrom = pattern.AppliedFrom,
                Slots = pattern.Slots != null ? pattern.Slots.Select(slot => new AvailabilitySlotDTO
                {
                    Type = slot.Type,
                    DayInWeek = slot.DayInWeek,
                    SlotIndex = slot.SlotIndex
                }).ToList() : new List<AvailabilitySlotDTO>()
            };
    }
}
