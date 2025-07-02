using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;
using FluentValidation;

namespace App.DTOs.BookingDTOs
{
    #region Request DTO
    public class LearnerTimeSlotRequestDTO
    {
        public string TutorId { get; set; } = string.Empty;
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
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }

        public static Expression<Func<LearnerTimeSlotRequest, LearnerTimeSlotResponseDTO>> Projection =>
            r => new LearnerTimeSlotResponseDTO
            {
                Id = r.Id,
                TutorId = r.TutorId,
                DayInWeek = r.DayInWeek,
                SlotIndex = r.SlotIndex
            };
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

        public static Expression<Func<IGrouping<TutorInfoKey, LearnerTimeSlotRequest>, TutorInfoDTO>> TutorInfoProjection =>
        g => new TutorInfoDTO
        {
            TutorId = g.Key.TutorId,
            TutorName = g.Key.TutorName,
            TutorAvatarUrl = g.Key.TutorAvatarUrl,
            LatestRequestTime = g.Max(r => r.CreatedAt)
        };

        public class TutorInfoKey
        {
            public string TutorId { get; set; } = string.Empty;
            public string TutorName { get; set; } = string.Empty;
            public string TutorAvatarUrl { get; set; } = string.Empty;
        }   
    }


    #endregion

    #region Mapping
    public static class LearnerTimeSlotDTOExtensions
    {
        public static List<LearnerTimeSlotRequest> ToEntities(
            this LearnerTimeSlotRequestDTO request, 
            string learnerId)
        {
            return request.TimeSlots
                .Select(slot => LearnerTimeSlotRequest.Create(
                    learnerId,
                    request.TutorId,
                    slot.DayInWeek,
                    slot.SlotIndex
                ))
                .ToList();
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

            RuleFor(x => x.TimeSlots)
                .NotEmpty()
                .WithMessage("At least one time slot is required.");

            RuleForEach(x => x.TimeSlots)
                .SetValidator(new TimeSlotPairValidator());
        }
    }
    #endregion
}
