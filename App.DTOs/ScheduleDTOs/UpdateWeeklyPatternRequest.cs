using FluentValidation;

namespace App.DTOs.ScheduleDTOs
{
    public class UpdateWeeklyPatternRequest
    {
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();
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

            RuleForEach(x => x.Slots).ChildRules(slot =>
            {
                slot.RuleFor(s => s.SlotIndex)
                    .InclusiveBetween(0, 47).WithMessage("SlotIndex must be between 0 and 47.");
                slot.RuleFor(s => s.DayInWeek)
                    .IsInEnum().WithMessage("Invalid DayInWeek.");
                slot.RuleFor(s => s.Type)
                    .IsInEnum().WithMessage("Invalid SlotType.");
            });
        }
    }
    #endregion
}
