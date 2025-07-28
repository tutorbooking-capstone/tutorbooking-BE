using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

namespace App.DTOs.BookingDTOs
{
    public class BookingListItemDTO
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorAvatarUrl { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string LearnerAvatarUrl { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int SlotCount { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime EarliestBookedDate { get; set; }
        
        public static BookingListItemDTO FromEntity(Booking booking)
        {
            return new BookingListItemDTO
            {
                Id = booking.Id,
                TutorId = booking.TutorId,
                TutorName = booking.Tutor?.User?.FullName ?? string.Empty,
                TutorAvatarUrl = booking.Tutor?.User?.ProfilePictureUrl ?? string.Empty,
                LearnerId = booking.LearnerId ?? string.Empty,
                LearnerName = booking.Learner?.User?.FullName ?? string.Empty,
                LearnerAvatarUrl = booking.Learner?.User?.ProfilePictureUrl ?? string.Empty,
                LessonName = booking.LessonSnapshot?.Name ?? string.Empty,
                TotalPrice = booking.BookedSlots?.Sum(bs => bs.HeldFund?.Amount ?? 0) ?? 0,
                SlotCount = booking.BookedSlots?.Count ?? 0,
                CreatedTime = booking.CreatedTime.DateTime,
                EarliestBookedDate = booking.BookedSlots?.OrderBy(bs => bs.BookedDate).FirstOrDefault()?.BookedDate ?? DateTime.MinValue
            };
        }
        
        public static Expression<Func<Booking, BookingListItemDTO>> Projection =>
            b => new BookingListItemDTO
            {
                Id = b.Id,
                TutorId = b.TutorId,
                TutorName = b.Tutor!.User.FullName,
                TutorAvatarUrl = b.Tutor!.User.ProfilePictureUrl,
                LearnerId = b.LearnerId!,
                LearnerName = b.Learner!.User!.FullName,
                LearnerAvatarUrl = b.Learner!.User.ProfilePictureUrl,
                LessonName = b.LessonSnapshot!.Name,
                TotalPrice = b.BookedSlots!.Sum(bs => bs.HeldFund!.Amount),
                SlotCount = b.BookedSlots!.Count,
                CreatedTime = b.CreatedTime.DateTime,
                EarliestBookedDate = b.BookedSlots!.OrderBy(bs => bs.BookedDate).FirstOrDefault()!.BookedDate
            };
    }
    
    // DTO cho chi tiết booking
    public class BookingDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorAvatarUrl { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string LearnerAvatarUrl { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string OriginalOfferId { get; set; } = string.Empty;
        public LessonSnapshotDTO LessonSnapshot { get; set; } = new LessonSnapshotDTO();
        public decimal TotalPrice { get; set; }
        public List<BookedSlotDetailDTO> BookedSlots { get; set; } = new List<BookedSlotDetailDTO>();
        public DateTime CreatedTime { get; set; }
        
        public static BookingDetailDTO FromEntity(Booking booking)
        {
            var result = new BookingDetailDTO
            {
                Id = booking.Id,
                TutorId = booking.TutorId,
                TutorName = booking.Tutor?.User?.FullName ?? string.Empty,
                TutorAvatarUrl = booking.Tutor?.User?.ProfilePictureUrl ?? string.Empty,
                LearnerId = booking.LearnerId ?? string.Empty,
                LearnerName = booking.Learner?.User?.FullName ?? string.Empty,
                LearnerAvatarUrl = booking.Learner?.User?.ProfilePictureUrl ?? string.Empty,
                Note = booking.Note ?? string.Empty,
                OriginalOfferId = booking.OriginalOfferId ?? string.Empty,
                CreatedTime = booking.CreatedTime.DateTime,
                TotalPrice = booking.BookedSlots?.Sum(bs => bs.HeldFund?.Amount ?? 0) ?? 0
            };
            
            if (booking.LessonSnapshot != null)
            {
                result.LessonSnapshot = LessonSnapshotDTO.FromEntity(booking.LessonSnapshot);
            }
            
            if (booking.BookedSlots != null)
            {
                result.BookedSlots = booking.BookedSlots
                    .OrderBy(bs => bs.BookedDate)
                    .Select(BookedSlotDetailDTO.FromEntity)
                    .ToList();
            }
            
            return result;
        }
    }
    
    // DTO cho LessonSnapshot
    public class LessonSnapshotDTO
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalLessonId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; }
        
        public static LessonSnapshotDTO FromEntity(LessonSnapshot lessonSnapshot)
        {
            return new LessonSnapshotDTO
            {
                Id = lessonSnapshot.Id,
                OriginalLessonId = lessonSnapshot.OriginalLessonId,
                Name = lessonSnapshot.Name,
                Description = lessonSnapshot.Description,
                Note = lessonSnapshot.Note ?? string.Empty,
                TargetAudience = lessonSnapshot.TargetAudience,
                Prerequisites = lessonSnapshot.Prerequisites,
                LanguageCode = lessonSnapshot.LanguageCode,
                Category = lessonSnapshot.Category,
                Price = lessonSnapshot.Price,
                Currency = lessonSnapshot.Currency,
                DurationInMinutes = lessonSnapshot.DurationInMinutes
            };
        }
    }
    
    // DTO cho chi tiết BookedSlot
    public class BookedSlotDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public int SlotIndex { get; set; }
        public string SlotNote { get; set; } = string.Empty;
        public SlotStatus Status { get; set; }
        public HeldFundDTO HeldFund { get; set; } = new HeldFundDTO();
        
        public static BookedSlotDetailDTO FromEntity(BookedSlot bookedSlot)
        {
            var result = new BookedSlotDetailDTO
            {
                Id = bookedSlot.Id,
                BookedDate = bookedSlot.BookedDate,
                SlotIndex = bookedSlot.SlotIndex,
                SlotNote = bookedSlot.SlotNote ?? string.Empty,
                Status = bookedSlot.Status
            };
            
            if (bookedSlot.HeldFund != null)
            {
                result.HeldFund = HeldFundDTO.FromEntity(bookedSlot.HeldFund);
            }
            
            return result;
        }
    }
    
    // DTO cho HeldFund
    public class HeldFundDTO
    {
        public string Id { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public HeldFundStatus Status { get; set; }
        public DateTime ReleaseAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public static HeldFundDTO FromEntity(HeldFund heldFund)
        {
            return new HeldFundDTO
            {
                Id = heldFund.Id,
                Amount = heldFund.Amount,
                Status = heldFund.Status,
                ReleaseAt = heldFund.ReleaseAt,
                ResolvedAt = heldFund.ResolvedAt,
                CreatedAt = heldFund.CreatedAt
            };
        }
    }
}