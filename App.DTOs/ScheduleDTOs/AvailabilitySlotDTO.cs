using App.Repositories.Models.Scheduling;
using System;

namespace App.DTOs.ScheduleDTOs
{
    public class AvailabilitySlotDTO
    {
        public SlotType Type { get; set; }
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }

        #region Validation
        public bool IsValid()
        {
            // Validate SlotIndex (0-47 cho 48 slot 30 phút trong ngày)
            if (SlotIndex < 0 || SlotIndex > 47)
                return false;

            // Validate DayInWeek (phải là giá trị hợp lệ trong enum)
            if (!Enum.IsDefined(typeof(DayInWeek), DayInWeek))
                return false;

            // Validate Type (phải là giá trị hợp lệ trong enum)
            if (!Enum.IsDefined(typeof(SlotType), Type))
                return false;

            return true;
        }
        #endregion

        #region Mapping
        public AvailabilitySlot ToEntity()
        {
            return new AvailabilitySlot
            {
                Type = this.Type,
                DayInWeek = this.DayInWeek,
                SlotIndex = this.SlotIndex
            };
        }

        public static IEnumerable<AvailabilitySlot> ToEntities(IEnumerable<AvailabilitySlotDTO> dtos)
        {
            return dtos.Select(dto => dto.ToEntity());
        }
        #endregion
    }
}
