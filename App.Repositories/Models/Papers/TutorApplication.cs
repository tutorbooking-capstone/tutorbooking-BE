using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models.Papers
{
    public class TutorApplication : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.UnSubmitted;
        public string RevisionNotes { get; set; } = string.Empty; // Notes for revisions requested by the admin/verifier 
        public string InternalNotes { get; set; } = string.Empty; // Internal notes for administrative use (not shown to tutors)

        public virtual Tutor? Tutor { get; set; }
        public virtual ICollection<Document>? Documents { get; set; }
        public virtual ICollection<ApplicationRevision>? ApplicationRevisions { get; set; }

        #region Behavior
        public static TutorApplication Create(string tutorId)
        {
            var newTutorApplication =  new TutorApplication
            {
                TutorId = tutorId,
                Status = ApplicationStatus.UnSubmitted,
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
        // Initial status when was tutor create 
        UnSubmitted = 0,

        // Initial status when tutor first submits application
        PendingVerification = 1,
        
        // Status when staff has requested changes to the application
        RevisionRequested = 2,
        
        // Status when tutor has submitted revised documents after a revision request
        PendingReverification = 3,
        
        // Status when all required documents have been verified
        Verified =4
    }
}
