using App.Core.Base;
using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    public class AppUser : BaseUser
    {
        public string FullName { get; set; } = "SystemCreated";
        public int? EmailCode { get; set; }
        public DateTime? CodeGeneratedTime { get; set; }
        
        public string ProfilePictureUrl { get; set; } = string.Empty;   
        public string ProfilePicturePublicId { get; set; } = string.Empty;


        #region Behavior
        public void UpdateProfile(
            string fullName,
            string? phoneNumber)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
        }

        public Expression<Func<AppUser, object>>[] UpdateProfilePicture(
            string? profilePictureUrl, 
            string? profilePicturePublicId)
        {
            if (ProfilePictureUrl == profilePictureUrl && ProfilePicturePublicId == profilePicturePublicId)
                return Array.Empty<Expression<Func<AppUser, object>>>();

            ProfilePictureUrl = profilePictureUrl ?? string.Empty;
            ProfilePicturePublicId = profilePicturePublicId ?? string.Empty;

            return
            [
                x => x.ProfilePictureUrl,
                x => x.ProfilePicturePublicId
            ];
        }
        public Tutor BecameTutor(string userId)
            => new Tutor
            {
                UserId = userId,
                VerificationStatus = VerificationStatus.Basic,
                BecameTutorAt = DateTime.UtcNow,
                LastStatusUpdateAt = DateTime.UtcNow,

                User = this
            };
        #endregion

    }
}
