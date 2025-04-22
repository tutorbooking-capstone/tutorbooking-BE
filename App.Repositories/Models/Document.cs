using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Document : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; //Description of document by Tutor
        public string? StaffId { get; set; } = null; //Exist if the hardcopy document is uploaded by staff
        public bool IsVisibleToLearner { get; set; } = false; // Whether the document is visible to learners
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Firebase-specific fields
        public string StoragePath { get; set; } = string.Empty; // Path in Firebase Storage (e.g., "documents/{applicationId}/{filename}")
        public string DownloadUrl { get; set; } = string.Empty; // Public URL to access the file

        public virtual TutorApplication Application { get; set; } = new();
        public virtual Staff Staff { get; set; } = new();
    }
}
