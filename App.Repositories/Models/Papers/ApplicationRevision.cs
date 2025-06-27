using App.Core.Base;
using App.Repositories.Models.Papers;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    // Entity class for tracking revision history and staff actions
    public class ApplicationRevision : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public RevisionAction Action { get; set; } // Type of action performed (request revision, approve, reject)
        public string Notes { get; set; } = string.Empty; // Detailed notes explaining the action taken

        public virtual TutorApplication? Application { get; set; }
        public virtual Staff? Staff { get; set; }

        #region Behavior
        public Expression<Func<ApplicationRevision, object>>[] UpdateAction(RevisionAction newAction)
        {
            if (Action == newAction)
                return Array.Empty<Expression<Func<ApplicationRevision, object>>>();

            Action = newAction;
            return [x => x.Action];
        }
        #endregion
    }

    public enum RevisionAction
    {
        // Staff requests the tutor to revise their application or documents
        RequestRevision = 0,

        // Staff approves the application or a specific document Upload or Hardcopy
        Approve = 1,

        // Staff rejects the application or a specific document
        Reject = 2
    }
}
