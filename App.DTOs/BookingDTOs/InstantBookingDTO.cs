using System.ComponentModel.DataAnnotations;

namespace App.DTOs.BookingDTOs
{
    public class InstantBookingRequest
    {
        [Required]
        public string TutorId { get; set; } = string.Empty;
        
        [Required]
        public string LessonId { get; set; } = string.Empty;
        
        [Required]
        public List<BookingSlotRequest> Slots { get; set; } = new();
    }

    public class BookingSlotRequest
    {
        public DateTime SlotDate { get; set; }
        public int SlotIndex { get; set; }
    }
}