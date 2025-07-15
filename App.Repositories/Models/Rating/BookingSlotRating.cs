using App.Core.Base;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models.Rating
{
    public class BookingSlotRating: BaseEntity
    {
        [ForeignKey(nameof(BookingSlot))]
        public string BookingSlotId { get; set; }
        public string TutorId { get; set; }
        public string LearnerId { get; set; }
        public float TeachingQuality { get; set; } = 1;
        public float Attitude { get; set; } = 1;
        public float Commitment { get; set; } = 1;
        public string? Comment { get; set; }


        public virtual BookingSlot? BookingSlot { get; set; }
        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
    }
}
