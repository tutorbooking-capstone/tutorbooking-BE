using App.Repositories.Models.User;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorResponse
    {  
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
    }

    #region Mapping
    public static class TutorResponseExtensions
    {
        public static TutorResponse ToTutorResponse(this Tutor tutor)
        {
            return new TutorResponse
            {
                UserId = tutor.UserId,
                Email = tutor.User?.Email ?? "N/A", 
                FullName = tutor.User?.FullName ?? "N/A", 
                VerificationStatus = tutor.VerificationStatus
            };
        }
    }
    #endregion
}
