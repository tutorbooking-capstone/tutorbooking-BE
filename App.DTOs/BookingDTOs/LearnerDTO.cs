using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using System.ComponentModel.DataAnnotations;

namespace App.DTOs.BookingDTOs
{
    public class AcceptOfferRequest
    {
        [Required]
        public string OfferId { get; set; } = string.Empty;
    }

    #region Booking Response
    public class BookingResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int SlotCount { get; set; }
        public List<BookedSlotDTO> BookedSlots { get; set; } = new List<BookedSlotDTO>();

        public static BookingResponse FromEntity(
            Booking booking, 
            LessonSnapshot lessonSnapshot, 
            List<BookedSlot> bookedSlots, 
            decimal totalPrice)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                TutorId = booking.TutorId,
                LearnerId = booking.LearnerId!,
                LessonName = lessonSnapshot.Name,
                TotalPrice = totalPrice,
                SlotCount = bookedSlots.Count,
                BookedSlots = bookedSlots.Select(bs => new BookedSlotDTO
                {
                    Id = bs.Id,
                    BookedDate = bs.BookedDate,
                    SlotIndex = bs.SlotIndex,
                    Status = bs.Status
                }).ToList()
            };
        }
    }

    public class BookedSlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public int SlotIndex { get; set; }
        public SlotStatus Status { get; set; }
    }
    #endregion
}