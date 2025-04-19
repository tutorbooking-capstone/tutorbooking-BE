using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    // Entity class for tracking revision history and staff actions
    public class ApplicationRevision : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;

        // Type of action performed (request revision, approve, reject)
        public RevisionAction Action { get; set; }

        // Detailed notes explaining the action taken
        public string Notes { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual TutorApplication Application { get; set; } = new();
        public virtual Staff Staff { get; set; } = new();
    }

    public enum RevisionAction
    {
        // Staff requests the tutor to revise their application or documents
        RequestRevision,

        // Staff approves the application or a specific document Upload or Hardcopy
        ApproveUpload,
        ApproveHardcopy,

        // Staff rejects the application or a specific document
        Reject
    }
}
