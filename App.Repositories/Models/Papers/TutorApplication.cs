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
        [EnumDescription("Chưa gửi hồ sơ")]
        UnSubmitted = 0,

        [EnumDescription("Đang chờ xác minh")]
        PendingVerification = 1,
        
        [EnumDescription("Yêu cầu chỉnh sửa")]
        RevisionRequested = 2,
        
        [EnumDescription("Đang chờ xác minh lại")]
        PendingReverification = 3,
        
        [EnumDescription("Đã xác minh")]
        Verified = 4
    }
}
