using App.Repositories.Models.Scheduling;
using FluentValidation;

public record WeeklyAvailabilityPatternDTO
{
    public DateTime AppliedFrom { get; init; }
    public List<DailyAvailabilityDTO> DailyAvailabilities { get; init; } = new();
}

public record DailyAvailabilityPatternDTO
{
    public DayInWeek Day { get; init; }
    public DateTime Date { get; init; }
    public List<int> TimeSlotIndex { get; init; } = new();

    public static DailyAvailabilityPatternDTO Create(DayInWeek day, DateTime date, List<int> timeSlotIndices)
    {
        return new DailyAvailabilityPatternDTO
        {
            Day = day,
            Date = date,
            TimeSlotIndex = timeSlotIndices
        };
    }
}

public record DailyAvailabilityDTO
{
    public DayInWeek Day { get; init; }
    public DateTime? Date { get; init; }
    public List<TimeSlotDTO> TimeSlots { get; init; } = new();
}

public record TimeSlotDTO
{
    public int SlotIndex { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public SlotType Type { get; init; }
    public string? BookingId { get; init; }
    public string? LearnerId { get; init; }
    public string? Note { get; init; }
}

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
        // We don't load slots here anymore
        return new WeeklyAvailabilityPatternDTO
        {
            AppliedFrom = pattern.AppliedFrom,
            DailyAvailabilities = new List<DailyAvailabilityDTO>()
        };
    }

    public static WeeklyAvailabilityPattern ToEntity(this WeeklyAvailabilityPatternDTO dto, string tutorId)
    {
        var pattern = new WeeklyAvailabilityPattern
        {
            TutorId = tutorId,
            AppliedFrom = dto.AppliedFrom,
            Slots = new List<AvailabilitySlot>()
        };

        foreach (var dailyAvailability in dto.DailyAvailabilities ?? Enumerable.Empty<DailyAvailabilityDTO>())
        {
            foreach (var slot in dailyAvailability.TimeSlots ?? Enumerable.Empty<TimeSlotDTO>())
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
