using App.Core.Base;
using App.Repositories.Models.User;

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
    }

    public enum RevisionAction
    {
        // Staff requests the tutor to revise their application or documents
        RequestRevision = 0,

        // Staff approves the application or a specific document Upload or Hardcopy
        ApproveUpload = 1,
        ApproveHardcopy = 2,

        // Staff rejects the application or a specific document
        Reject = 3
    }
}
