using App.Repositories.Models.Scheduling;
using FluentValidation;

public record WeeklyAvailabilityPatternDTO(
    DateTime AppliedFrom,
    List<DailyAvailabilityDTO> DailyAvailabilities
);

public record DailyAvailabilityDTO(
    DayInWeek Day,
    List<TimeSlotDTO> TimeSlots
);

public record TimeSlotDTO(
    int SlotIndex,
    SlotType Type
);

#region Validators
public class WeeklyAvailabilityPatternDTOValidator : AbstractValidator<WeeklyAvailabilityPatternDTO>
{
    public WeeklyAvailabilityPatternDTOValidator()
    {
        // Rule 1: AppliedFrom must be at least tomorrow
        RuleFor(x => x.AppliedFrom)
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("Pattern can only be applied starting from tomorrow.");

        // Rule 2: AppliedFrom must be a Monday
        RuleFor(x => x.AppliedFrom)
            .Must(date => date.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("Pattern must start on a Monday.");

        // Validate daily availability
        RuleForEach(x => x.DailyAvailabilities)
            .SetValidator(new DailyAvailabilityDTOValidator());
    }
}

public class DailyAvailabilityDTOValidator : AbstractValidator<DailyAvailabilityDTO>
{
    public DailyAvailabilityDTOValidator()
    {
        // Validate day of week is valid
        RuleFor(x => x.Day)
            .IsInEnum()
            .WithMessage("Invalid day of week.");

        // Validate time slots
        RuleForEach(x => x.TimeSlots)
            .SetValidator(new TimeSlotDTOValidator());
            
        // Each slot index should be unique
        RuleFor(x => x.TimeSlots)
            .Must(slots => slots.Select(s => s.SlotIndex).Distinct().Count() == slots.Count)
            .WithMessage("Duplicate slot indices are not allowed.");
    }
}

public class TimeSlotDTOValidator : AbstractValidator<TimeSlotDTO>
{
    public TimeSlotDTOValidator()
    {
        // Validate slot index is between 0-47
        RuleFor(x => x.SlotIndex)
            .InclusiveBetween(0, 47)
            .WithMessage("Slot index must be between 0 and 47.");

        // Validate slot type is valid
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid slot type.");
    }
}
#endregion

public static class WeeklyAvailabilityPatternExtensions
{
    public static WeeklyAvailabilityPatternDTO ToDTO(this WeeklyAvailabilityPattern pattern)
    {
        var dailySlots = pattern.Slots
            .GroupBy(s => s.DayInWeek)
            .Select(g => new DailyAvailabilityDTO(
                g.Key,
                g.Select(s => new TimeSlotDTO(
                    s.SlotIndex,
                    s.Type
                )).ToList()
            ))
            .ToList();

        return new WeeklyAvailabilityPatternDTO(
            pattern.AppliedFrom,
            dailySlots
        );
    }

    public static WeeklyAvailabilityPattern ToEntity(this WeeklyAvailabilityPatternDTO dto, string tutorId)
    {
        var pattern = new WeeklyAvailabilityPattern
        {
            TutorId = tutorId,
            AppliedFrom = dto.AppliedFrom,
            Slots = new List<AvailabilitySlot>()
        };

        foreach (var dailyAvailability in dto.DailyAvailabilities)
        {
            foreach (var slot in dailyAvailability.TimeSlots)
            {
                pattern.Slots.Add(new AvailabilitySlot
                {
                    DayInWeek = dailyAvailability.Day,
                    SlotIndex = slot.SlotIndex,
                    Type = slot.Type,
                    WeeklyPatternId = pattern.Id
                });
            }
        }

        return pattern;
    }
}
