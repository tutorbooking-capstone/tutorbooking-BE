using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class TutorApplication : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.PendingVerification;
        public string RevisionNotes { get; set; } = string.Empty; // Notes for revisions requested by the admin/verifier 
        public string InternalNotes { get; set; } = string.Empty; // Internal notes for administrative use (not shown to tutors)

        public virtual Tutor? Tutor { get; set; }
    }

    public enum ApplicationStatus
    {
        // Initial status when tutor first submits application
        PendingVerification,
        
        // Status when staff has requested changes to the application
        RevisionRequested,
        
        // Status when tutor has submitted revised documents after a revision request
        PendingReverification,
        
        // Status when digital documents have been verified, but physical docs still needed
        VerifiedUpload,
        
        // Final status when both digital and physical documents are verified
        VerifiedHardcopy
    }
}
