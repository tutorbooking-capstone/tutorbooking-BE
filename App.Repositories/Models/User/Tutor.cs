namespace App.Repositories.Models.User
{
    public class Tutor
    {
        public string UserId { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotStarted;
        public DateTime? LastStatusUpdateAt { get; set; }

        public virtual AppUser User { get; set; } = new();
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
