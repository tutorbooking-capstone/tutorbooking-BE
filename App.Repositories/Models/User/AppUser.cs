using App.Core.Base;
using App.Repositories.Models.Chat;
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
        public string Timezone { get; set; } = "UTC+7";

        //Navigation Properties
        public virtual ICollection<ChatConversationReadStatus>? ChatConversationReadStatuses { get; set; }

        #region Behavior
        public void UpdateBasicInformation(
            string fullName,
            string? phoneNumber)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
        }

        public Expression<Func<AppUser, object>>[] UpdateBasicInformationPicture(
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
            return [x => x.DateOfBirth!];
        }

        public Expression<Func<AppUser, object>>[] UpdateGender(Gender gender)
        {
            if (Gender == gender)
                return Array.Empty<Expression<Func<AppUser, object>>>();

            Gender = gender;
            return [x => x.Gender];
        }

        public Expression<Func<AppUser, object>>[] UpdateBasicInformation(
            string? fullName,
            DateTime? dateOfBirth,
            Gender? gender,
            string? timezone = null)
        {
            var updatedFields = new List<Expression<Func<AppUser, object>>>();

            if (fullName != null && FullName != fullName)
            {
                FullName = fullName;
                updatedFields.Add(x => x.FullName);
            }

            if (dateOfBirth.HasValue && DateOfBirth != dateOfBirth)
            {
                DateOfBirth = dateOfBirth;
                updatedFields.Add(x => x.DateOfBirth!);
            }

            if (gender.HasValue && Gender != gender)
            {
                Gender = gender.Value;
                updatedFields.Add(x => x.Gender);
            }

            if (timezone != null && Timezone != timezone)
            {
                Timezone = timezone;
                updatedFields.Add(x => x.Timezone);
            }

            return updatedFields.ToArray();
        }

        // public Tutor BecameTutor(string userId)
        //     => new Tutor
        //     {
        //         UserId = userId,
        //         VerificationStatus = VerificationStatus.Basic,
        //         BecameTutorAt = DateTime.UtcNow,
        //         LastStatusUpdateAt = DateTime.UtcNow,

        //         User = this
        //     };

        public Learner BecomeLearner(string userId)
            => new Learner
            {
                UserId = userId,
                LanguageCode = string.Empty,
                ProficiencyLevel = 1,
                
                User = this
            };
        #endregion
    }
}
