using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;
using FluentValidation;
using System.Text.Json;

namespace App.DTOs.BookingDTOs
{
    #region Request DTO
    public class LearnerTimeSlotRequestDTO
    {
        public string TutorId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public List<TimeSlotPair> TimeSlots { get; set; } = new();
    }

    public class TimeSlotPair
    {
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
    }
    #endregion

    #region Response DTO
    public class LearnerTimeSlotResponseDTO
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public List<TimeSlotPair> TimeSlots { get; set; } = new();

        public static LearnerTimeSlotResponseDTO FromEntity(LearnerTimeSlotRequest request)
        {
            var slots = JsonSerializer.Deserialize<List<TimeSlotPair>>(request.RequestedSlotsJson) ?? new List<TimeSlotPair>();
            return new LearnerTimeSlotResponseDTO
            {
                Id = request.Id,
                TutorId = request.TutorId,
                LearnerId = request.LearnerId,
                LessonId = request.LessonId,
                ExpectedStartDate = request.ExpectedStartDate,
                TimeSlots = slots
            };
        }
    }
    #endregion

    #region Tutor View DTOs
    public class LearnerTimeSlotWithLearnerInfoDTO
    {
        public string Id { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
    }

    public class LearnerInfoDTO
    {
        public string LearnerId { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public bool HasUnviewed { get; set; }
        public DateTime LatestRequestTime { get; set; }
    }
    #endregion

    #region Tutor Info DTO
    public class TutorInfoDTO
    {
        public string TutorId { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorAvatarUrl { get; set; } = string.Empty;
        public DateTime LatestRequestTime { get; set; }
        public string TutorBookingOfferId { get; set; } = string.Empty;

        public static Expression<Func<IGrouping<TutorInfoKey, LearnerTimeSlotRequest>, TutorInfoDTO>> TutorInfoProjection =>
        g => new TutorInfoDTO
        {
            TutorId = g.Key.TutorId,
            TutorName = g.Key.TutorName,
            TutorAvatarUrl = g.Key.TutorAvatarUrl,
            LatestRequestTime = g.Max(r => r.CreatedAt),
            TutorBookingOfferId = g.Key.TutorBookingOfferId
        };

        public class TutorInfoKey
        {
            public string TutorId { get; set; } = string.Empty;
            public string TutorName { get; set; } = string.Empty;
            public string TutorAvatarUrl { get; set; } = string.Empty;
            public string TutorBookingOfferId { get; set; } = string.Empty;
        }   
    }


    #endregion

    #region Mapping
    public static class LearnerTimeSlotDTOExtensions
    {
        public static LearnerTimeSlotRequest ToEntity(
            this LearnerTimeSlotRequestDTO request,
            string learnerId)
        {
            var requestedSlots = request.TimeSlots
                .Select(slot => new RequestedSlot
                {
                    DayInWeek = slot.DayInWeek,
                    SlotIndex = slot.SlotIndex
                });

            return LearnerTimeSlotRequest.Create(
                learnerId,
                request.TutorId,
                request.LessonId,
                request.ExpectedStartDate,
                requestedSlots
            );
        }
    }
    #endregion

    #region Validators
    public class TimeSlotPairValidator : AbstractValidator<TimeSlotPair>
    {
        public TimeSlotPairValidator()
        {
            RuleFor(x => x.DayInWeek)
                .IsInEnum()
                .WithMessage("Invalid day of week.");

            RuleFor(x => x.SlotIndex)
                .InclusiveBetween(0, 47)
                .WithMessage("Slot index must be between 0 and 47.");
        }
    }

    public class LearnerTimeSlotRequestDTOValidator : AbstractValidator<LearnerTimeSlotRequestDTO>
    {
        public LearnerTimeSlotRequestDTOValidator()
        {
            RuleFor(x => x.TutorId)
                .NotEmpty()
                .WithMessage("Tutor ID is required.");

            RuleFor(x => x.LessonId)
                .NotEmpty()
                .When(x => x.LessonId != null)
                .WithMessage("Lesson ID cannot be an empty string if provided.");
            RuleFor(x => x.ExpectedStartDate)
                .NotEmpty()
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("Expected start date cannot be in the past.");

            RuleFor(x => x.TimeSlots)
                .NotEmpty()
                .WithMessage("At least one time slot is required.");

            RuleForEach(x => x.TimeSlots)
                .SetValidator(new TimeSlotPairValidator());
        }
    }
    #endregion
}
