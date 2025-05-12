using App.Repositories.Models;
using App.Repositories.Models.User;

namespace App.DTOs.UserDTOs
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        
        // Learner specific data
        public string LearningLanguageCode { get; set; } = string.Empty;
        public int LearningProficiencyLevel { get; set; }
    }

    public static class UserProfileResponseExtensions
    {
        public static UserProfileResponse ToUserProfileResponse(this AppUser user, Learner? learner = null)
        {
            var response = new UserProfileResponse
            {
                Id = Guid.TryParse(user.Id, out var id) ? id : Guid.Empty,
                FullName = user.FullName ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                ProfileImageUrl = user.ProfilePictureUrl,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Timezone = user.Timezone
            };

            if (learner != null)
            {
                response.LearningLanguageCode = learner.LanguageCode;
                response.LearningProficiencyLevel = learner.ProficiencyLevel;
            }

            return response;
        }
    }
}
