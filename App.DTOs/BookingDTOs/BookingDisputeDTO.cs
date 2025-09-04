using App.Repositories.Models;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json;

namespace App.DTOs.BookingDTOs
{
    #region Request DTOs
    public class StaffDisputeFilterRequest
    {
        public List<DisputeResolution>? ResolutionFilter { get; set; } = null;  
        public string? CaseNumber { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }

    public class CreateDisputeRequest
    {
        [Required]
        public string BookedSlotId { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Reason { get; set; } = string.Empty;

        public List<string>? EvidenceUrls { get; set; }
    }

    public class RespondToDisputeRequest
    {
        [Required]
        public string DisputeId { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Response { get; set; } = string.Empty;

        [Required]
        public DisputeResolution Resolution { get; set; }
    }

    public class WithdrawDisputeRequest
    {
        [Required]
        public string DisputeId { get; set; } = string.Empty;
    }

    public class ResolveDisputeRequest
    {
        [Required]
        public string DisputeId { get; set; } = string.Empty;

        [Required]
        public DisputeResolution Resolution { get; set; }

        public string? Notes { get; set; }
    }
    #endregion

    #region Response DTOs
    public class BookingDisputeResponse
    {
        public string Id { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string BookedSlotId { get; set; } = string.Empty;
        public BookingUserInfo? Learner { get; set; }
        public BookingUserInfo? Tutor { get; set; }
        public string LearnerReason { get; set; } = string.Empty;
        public string? TutorResponse { get; set; }
        public DisputeStatus Status { get; set; }
        public DisputeResolution Resolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ReconciliationEndTime { get; set; }
        public DateTime? TutorRespondedAt { get; set; }
        public DateTime? StaffReviewEndTime { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public List<string>? EvidenceUrls { get; set; }
        public string? StaffNotes { get; set; }

        private static List<string>? DeserializeEvidenceUrls(string? urlsJson)
        {
            if (string.IsNullOrEmpty(urlsJson))
                return null;
            return JsonSerializer.Deserialize<List<string>>(urlsJson, new JsonSerializerOptions());
        }

        public static Expression<Func<BookingDispute, BookingDisputeResponse>> Projection =>
            d => new BookingDisputeResponse
            {
                Id = d.Id,
                CaseNumber = d.CaseNumber,
                BookedSlotId = d.BookedSlotId,
                Learner = BookingUserInfo.FromUser(d.Learner),
                Tutor = BookingUserInfo.FromUser(d.Tutor),
                LearnerReason = d.LearnerReason,
                TutorResponse = d.TutorResponse,
                Status = d.Status,
                Resolution = d.Resolution,
                CreatedAt = d.CreatedAt,
                ReconciliationEndTime = d.ReconciliationEndTime,
                TutorRespondedAt = d.TutorRespondedAt,
                StaffReviewEndTime = d.StaffReviewEndTime,
                ResolvedAt = d.ResolvedAt,
                EvidenceUrls = DeserializeEvidenceUrls(d.EvidenceUrls),
                StaffNotes = d.StaffNotes
            };
    }

    public class DisputeDetailResponse
    {
        public BookingDisputeResponse Dispute { get; set; } = new();
        public List<BookedSlotDTO> AffectedSlots { get; set; } = new();
        public decimal DisputedAmount { get; set; }
    }
    #endregion

    #region Validators
    public class CreateDisputeRequestValidator : AbstractValidator<CreateDisputeRequest>
    {
        public CreateDisputeRequestValidator()
        {
            RuleFor(x => x.BookedSlotId)
                .NotEmpty().WithMessage("ID của bookedSlot không được để trống.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Lý do khiếu nại không được để trống.")
                .MinimumLength(10).WithMessage("Lý do khiếu nại phải có ít nhất 10 ký tự.");
        }
    }

    public class RespondToDisputeRequestValidator : AbstractValidator<RespondToDisputeRequest>
    {
        public RespondToDisputeRequestValidator()
        {
            RuleFor(x => x.DisputeId)
                .NotEmpty().WithMessage("ID của khiếu nại không được để trống.");

            RuleFor(x => x.Response)
                .NotEmpty().WithMessage("Phản hồi không được để trống.")
                .MinimumLength(10).WithMessage("Phản hồi phải có ít nhất 10 ký tự.");
        }
    }
    #endregion
}