using System.ComponentModel.DataAnnotations;
using App.Repositories.Models.Scheduling;

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