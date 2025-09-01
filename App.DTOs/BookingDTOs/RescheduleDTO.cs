using App.Repositories.Models;
using System.Linq.Expressions;

namespace App.DTOs.BookingDTOs
{
    public class RescheduleRequestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string BookedSlotId { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty;
        public string Initiator { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? ResponseNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? AcceptedSlotId { get; set; }
        public BookingUserInfo? Tutor { get; set; }
        public BookingUserInfo? Learner { get; set; }
        public BookedSlotInfo OriginalSlot { get; set; } = new();
        public List<OfferedSlotDTO> OfferedSlots { get; set; } = new();

        // Expression for EF projection
        public static Expression<Func<RescheduleRequest, RescheduleRequestResponse>> Projection =>
            r => new RescheduleRequestResponse
            {
                Id = r.Id,
                BookedSlotId = r.BookedSlotId,
                RequestedByUserId = r.RequestedByUserId,
                Initiator = r.Initiator.ToString(),
                Reason = r.Reason,
                Status = (int)r.Status,
                ResponseNote = r.ResponseNote,
                CreatedAt = r.CreatedAt,
                ExpiresAt = r.ExpiresAt,
                RespondedAt = r.RespondedAt,
                AcceptedSlotId = r.AcceptedSlotId,
                Tutor = r.BookedSlot != null && r.BookedSlot.Booking != null && r.BookedSlot.Booking.Tutor != null ?
                    new BookingUserInfo
                    {
                        Id = r.BookedSlot.Booking.TutorId,
                        FullName = r.BookedSlot.Booking.Tutor.User!.FullName,
                        ProfilePictureUrl = r.BookedSlot.Booking.Tutor.User!.ProfilePictureUrl,
                        Gender = r.BookedSlot.Booking.Tutor.User!.Gender
                    } : null,
                Learner = r.BookedSlot != null && r.BookedSlot.Booking != null && r.BookedSlot.Booking.Learner != null ?
                    new BookingUserInfo
                    {
                        Id = r.BookedSlot.Booking.LearnerId!,
                        FullName = r.BookedSlot.Booking.Learner.User!.FullName,
                        ProfilePictureUrl = r.BookedSlot.Booking.Learner.User!.ProfilePictureUrl,
                        Gender = r.BookedSlot.Booking.Learner.User!.Gender
                    } : null,
                OriginalSlot = r.BookedSlot != null ?
                    new BookedSlotInfo
                    {
                        Id = r.BookedSlot.Id,
                        BookingId = r.BookedSlot.BookingId,
                        BookedDate = r.BookedSlot.BookedDate,
                        SlotIndex = r.BookedSlot.SlotIndex,
                        Status = r.BookedSlot.Status.ToString()
                    } : new BookedSlotInfo(),
                OfferedSlots = r.OfferedSlots.Select(os => new OfferedSlotDTO
                {
                    SlotDateTime = os.SlotDateTime,
                    SlotIndex = os.SlotIndex
                }).ToList()
            };

        // Method for in-memory conversion (if needed)
        public static RescheduleRequestResponse FromEntity(RescheduleRequest r)
        {
            var func = Projection.Compile();
            return func(r);
        }
    }

    public class BookedSlotInfo
    {
        public string Id { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public int SlotIndex { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateRescheduleRequest
    {
        public string BookedSlotId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime NewSlotDateTime { get; set; }
        public int NewSlotIndex { get; set; }
    }
}