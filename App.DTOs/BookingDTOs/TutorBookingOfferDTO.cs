using App.Repositories.Models.Booking;
using App.Repositories.Models.User;
using FluentValidation;
using System.Linq.Expressions;

namespace App.DTOs.BookingDTOs
{
    #region Response DTOs
    public class BookingUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public Gender Gender { get; set; }

        public static BookingUserInfo? FromUser(Tutor? tutor)
        {
            return tutor != null && tutor.User != null ? new BookingUserInfo
            {
                Id = tutor.UserId,
                FullName = tutor.User.FullName,
                ProfilePictureUrl = tutor.User.ProfilePictureUrl,
                Gender = tutor.User.Gender
            } : null;
        }

        public static BookingUserInfo? FromUser(Learner? learner)
        {
            return learner != null && learner.User != null ? new BookingUserInfo
            {
                Id = learner.UserId,
                FullName = learner.User.FullName,
                ProfilePictureUrl = learner.User.ProfilePictureUrl,
                Gender = learner.User.Gender
            } : null;
        }
    }

    public class TutorBookingOfferResponse
    {
        public string Id { get; set; } = string.Empty;
        public decimal PricePerSlot { get; set; }
        public int DurationInMinutes { get; set; }
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookingUserInfo? Tutor { get; set; }
        public BookingUserInfo? Learner { get; set; }
        public List<OfferedSlotDTO> OfferedSlots { get; set; } = new List<OfferedSlotDTO>();

        public static Expression<Func<TutorBookingOffer, TutorBookingOfferResponse>> Projection =>
            o => new TutorBookingOfferResponse
            {
                Id = o.Id,
                TotalPrice = o.TotalPrice,
                CreatedAt = o.CreatedAt,
                LessonId = o.Lesson != null ? o.Lesson.Id : "",
                LessonName = o.Lesson != null ? o.Lesson.Name : "N/A",
                PricePerSlot = o.Lesson != null ? o.Lesson.Price : 0,
                DurationInMinutes = o.Lesson != null ? o.Lesson.DurationInMinutes : 0,
                Tutor = BookingUserInfo.FromUser(o.Tutor),
                Learner = BookingUserInfo.FromUser(o.Learner),
                OfferedSlots = o.OfferedSlots.OrderBy(s => s.SlotDateTime).Select(s => new OfferedSlotDTO
                {
                    SlotDateTime = s.SlotDateTime,
                    SlotIndex = s.SlotIndex
                }).ToList()
            };
    }
    #endregion

    #region Request DTOs
    public class CreateTutorBookingOfferRequest
    {
        public string LearnerId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public List<OfferedSlotDTO> OfferedSlots { get; set; } = new();
    }

    public class UpdateTutorBookingOfferRequest
    {
        public List<OfferedSlotDTO> OfferedSlots { get; set; } = new();
    }

    public class OfferedSlotDTO
    {
        public DateTime SlotDateTime { get; set; }
        public int SlotIndex { get; set; }
    }
    #endregion

    #region Validators
    public class OfferedSlotDTOValidator : AbstractValidator<OfferedSlotDTO>
    {
        public OfferedSlotDTOValidator()
        {
            RuleFor(x => x.SlotIndex)
                .InclusiveBetween(0, 47)
                .WithMessage("Chỉ số của slot phải nằm trong khoảng từ 0 đến 47.");

            RuleFor(x => x.SlotDateTime)
                .NotEmpty().WithMessage("Ngày giờ của slot không được để trống.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Ngày giờ của slot phải ở trong tương lai.");
        }
    }

    public class CreateTutorBookingOfferRequestValidator : AbstractValidator<CreateTutorBookingOfferRequest>
    {
        public CreateTutorBookingOfferRequestValidator()
        {
            RuleFor(x => x.LearnerId)
                .NotEmpty().WithMessage("ID của học viên không được để trống.");

            RuleFor(x => x.LessonId)
                .NotEmpty().WithMessage("ID của bài học không được để trống.");

            RuleFor(x => x.OfferedSlots)
                .NotEmpty().WithMessage("Cần có ít nhất một slot được đề nghị.");

            RuleForEach(x => x.OfferedSlots)
                .SetValidator(new OfferedSlotDTOValidator());
        }
    }

    public class UpdateTutorBookingOfferRequestValidator : AbstractValidator<UpdateTutorBookingOfferRequest>
    {
        public UpdateTutorBookingOfferRequestValidator()
        {
            RuleFor(x => x.OfferedSlots)
                .NotEmpty().WithMessage("Cần có ít nhất một slot được đề nghị.");

            RuleForEach(x => x.OfferedSlots)
                .SetValidator(new OfferedSlotDTOValidator());
        }
    }
    #endregion
}
