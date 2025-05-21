using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    public class Tutor
    {
        public string UserId { get; set; } = string.Empty;

        // Tutor Info
        public string NickName { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TeachingMethod { get; set; } = string.Empty;

        // Verification Info
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Basic;
        public DateTime LastStatusUpdateAt { get; set; }
        public DateTime? BecameTutorAt { get; set; }

        public virtual AppUser? User { get; set; }

        #region Behavior
        public Expression<Func<Tutor, object>>[] UpdateVerificationStatus(VerificationStatus newStatus)
        {
            if (VerificationStatus == newStatus)
                return Array.Empty<Expression<Func<Tutor, object>>>();

            VerificationStatus = newStatus;
            LastStatusUpdateAt = DateTime.UtcNow;
            
            return
            [
                x => x.VerificationStatus,
                x => x.LastStatusUpdateAt
            ];
        }

        public Expression<Func<Tutor, object>>[] UpdateTutorProfile(
            string? nickName,
            string? brief,
            string? description,
            string? teachingMethod)
        {
            var updatedFields = new List<Expression<Func<Tutor, object>>>();

            if (nickName != null && NickName != nickName)
            {
                NickName = nickName;
                updatedFields.Add(x => x.NickName);
            }

            if (brief != null && Brief != brief)
            {
                Brief = brief;
                updatedFields.Add(x => x.Brief);
            }

            if (description != null && Description != description)
            {
                Description = description;
                updatedFields.Add(x => x.Description);
            }

            if (teachingMethod != null && TeachingMethod != teachingMethod)
            {
                TeachingMethod = teachingMethod;
                updatedFields.Add(x => x.TeachingMethod);
            }

            return updatedFields.ToArray();
        }
        #endregion      
    }

    public enum VerificationStatus
    {
        Basic = 0,           // Not Started (Gray Check)
        VerifiedUpload = 1,       // Verified via Documents (White Check)
        VerifiedHardcopy = 2,     // Verified via Hardcopy (Blue Check)
        //PendingDocuments,     // Awaiting Documents
        //PendingVerification,  // Pending Verification (Gray Check)
        //PendingReverification, // Pending Re-verification After Update
        //RevisionRequested     // Revision Requested (Red Check)
    }
}
