using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Document : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        
        //Description of document by Tutor
        public string Description { get; set; } = string.Empty;
        
        //Exist if the hardcopy document is uploaded by staff
        public string? StaffId { get; set; } = null;
        
        // Whether the document is visible to learners
        public bool IsVisibleToLearner { get; set; } = false; 

        //Type of document
        public string ContentType { get; set; } = string.Empty;

        //Size of document
        public long FileSize { get; set; }

        //Uploaded at
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Firebase-specific fields

        // Path in Firebase Storage (e.g., "documents/{applicationId}/{filename}")
        public string StoragePath { get; set; } = string.Empty;

        // Public URL to access the file
        public string DownloadUrl { get; set; } = string.Empty;

        public virtual TutorApplication Application { get; set; } = new();
        public virtual Staff Staff { get; set; } = new();
    }
}
