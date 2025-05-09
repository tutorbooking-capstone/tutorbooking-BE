using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    public class Tutor
    {
        public string UserId { get; set; } = string.Empty;

        //Display Name

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
