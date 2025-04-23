using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class AppUser : BaseUser
    {
        public string FullName { get; set; } = "SystemCreated";

        public int? EmailCode { get; set; }
        public DateTime? CodeGeneratedTime { get; set; }

        #region Behavior
        public void UpdateProfile(
            string fullName, 
            string? phoneNumber, 
            string updatedBy)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            this.TrackUpdate(updatedBy); 
        }

        public Tutor BecameTutor(string userId)
            => new Tutor {
                UserId = userId,
                VerificationStatus = VerificationStatus.NotStarted,
                BecameTutorAt = DateTime.UtcNow,
                LastStatusUpdateAt = DateTime.UtcNow,

                User = this
            };
        #endregion

    }
}
