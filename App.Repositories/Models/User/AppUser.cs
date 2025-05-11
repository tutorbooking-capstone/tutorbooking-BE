using App.Core.Base;
using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    #region Enums
    public enum Gender
    {
        NotGiven = 0,
        Male = 1,
        Female = 2
    }
    #endregion
    public class AppUser : BaseUser
    {
        public string FullName { get; set; } = string.Empty;
        public int? EmailCode { get; set; }
        public DateTime? CodeGeneratedTime { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;   
        public string ProfilePicturePublicId { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; } = Gender.NotGiven;

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

        public Expression<Func<AppUser, object>>[] UpdateFullName(string fullName)
        {
            if (FullName == fullName)
                return Array.Empty<Expression<Func<AppUser, object>>>();

            FullName = fullName;
            return [x => x.FullName];
        }

        public Expression<Func<AppUser, object>>[] UpdateDateOfBirth(DateTime? dateOfBirth)
        {
            if (DateOfBirth == dateOfBirth || dateOfBirth == null)
                return Array.Empty<Expression<Func<AppUser, object>>>();

            DateOfBirth = dateOfBirth;
            return [x => x.DateOfBirth];
        }

        public Expression<Func<AppUser, object>>[] UpdateGender(Gender gender)
        {
            if (Gender == gender)
                return Array.Empty<Expression<Func<AppUser, object>>>();

            Gender = gender;
            return [x => x.Gender];
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
