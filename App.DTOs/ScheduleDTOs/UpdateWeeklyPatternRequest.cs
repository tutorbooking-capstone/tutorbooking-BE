using App.Repositories.Models.Scheduling;
using FluentValidation;

namespace App.DTOs.ScheduleDTOs
{
    public class UpdateWeeklyPatternRequest
    {
        public DateTime AppliedFrom { get; set; }
        public List<SimpleAvailabilitySlotDTO> Slots { get; set; } = new();

        // DTO lồng bên trong để đơn giản hóa request
        public class SimpleAvailabilitySlotDTO
        {
            public DayInWeek DayInWeek { get; set; }
            public int SlotIndex { get; set; }
        }
    }

    #region Validator
    public class UpdateWeeklyPatternRequestValidator : AbstractValidator<UpdateWeeklyPatternRequest>
    {
        public UpdateWeeklyPatternRequestValidator()
        {
            RuleFor(x => x.AppliedFrom)
                .NotEmpty().WithMessage("AppliedFrom date is required.");

            RuleFor(x => x.Slots)
                .NotNull().WithMessage("Slots list cannot be null.")
                .NotEmpty().WithMessage("At least one availability slot is required.");

            // Cập nhật validator để kiểm tra DTO mới
            RuleForEach(x => x.Slots).ChildRules(slot =>
            {
                slot.RuleFor(s => s.SlotIndex)
                    .InclusiveBetween(0, 47).WithMessage("SlotIndex must be between 0 and 47.");
                slot.RuleFor(s => s.DayInWeek)
                    .IsInEnum().WithMessage("Invalid DayInWeek.");
            });
        }
    }
    #endregion
}
