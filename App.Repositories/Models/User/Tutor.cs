using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    public class Tutor
    {
        public string UserId { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotStarted;
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
        NotStarted,           // Not Started
        PendingDocuments,     // Awaiting Documents
        PendingVerification,  // Pending Verification (Gray Check)
        PendingReverification, // Pending Re-verification After Update
        VerifiedUpload,       // Verified via Documents (Yellow Check)
        VerifiedHardcopy,     // Verified via Hardcopy (Blue Check)
        RevisionRequested     // Revision Requested (Red Check)
    }
}
