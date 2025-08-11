namespace App.DTOs.RatingDTOs
{
    public class TutorRatingResponse
    {
        public string TutorId { get; set; } = null!;
        public float AverageTeachingQuality { get; set; } = 0;
        public float AverageAttitude { get; set; } = 0;
        public float AverageCommitment { get; set; } = 0;
        public object Reviews { get; set; } = null!;
    }
}
