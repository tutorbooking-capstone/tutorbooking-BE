using App.Repositories.Models.User;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorRegistrationProfileResponse
    {
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string Timezone { get; set; } = "UTC+7";
    }

    public static class TutorRegistrationProfileResponseExtensions
    {
        public static TutorRegistrationProfileResponse ToTutorRegistrationProfileResponse(this AppUser user)
        {
            return new TutorRegistrationProfileResponse
            {
                ProfileImageUrl = user.ProfilePictureUrl ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Timezone = user.Timezone
            };
        }
    }
}