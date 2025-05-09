using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models.Papers
{
    public class TutorApplication : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.PendingVerification;
        public string RevisionNotes { get; set; } = string.Empty; // Notes for revisions requested by the admin/verifier 
        public string InternalNotes { get; set; } = string.Empty; // Internal notes for administrative use (not shown to tutors)

        public virtual Tutor? Tutor { get; set; }

        #region Behavior
        public static TutorApplication Create(string tutorId)
        {
            var newTutorApplication =  new TutorApplication
            {
                TutorId = tutorId,
                Status = ApplicationStatus.PendingVerification,
                SubmittedAt = DateTime.UtcNow,
            };

            return newTutorApplication;
        }

        public Expression<Func<TutorApplication, object>>[] UpdateApplicationStatus(ApplicationStatus newStatus)
        {
            if (Status == newStatus)
                return Array.Empty<Expression<Func<TutorApplication, object>>>();

            Status = newStatus;
            return [x => x.Status];
        }
        #endregion
    }

    public enum ApplicationStatus
    {
        // Initial status when tutor first submits application
        PendingVerification = 0,
        
        // Status when staff has requested changes to the application
        RevisionRequested = 1,
        
        // Status when tutor has submitted revised documents after a revision request
        PendingReverification = 2,
        
        // Status when digital documents have been verified, but physical docs still needed
        VerifiedUpload = 3,
        
        // Final status when both digital and physical documents are verified
        VerifiedHardcopy = 4
    }
}
