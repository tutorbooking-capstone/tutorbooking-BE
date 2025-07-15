using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.RatingDTOs
{
    public class TutorRatingResponse
    {
        public string TutorId { get; set; } = null!;
        public float AverageTeachingQuality { get; set; } = 0;
        public float AverageAttitude { get; set; } = 0;
        public float AverageCommitment { get; set; } = 0;
    }
}
