using App.Core.Base;
using App.Repositories.Models.Papers;

namespace App.Repositories.Models
{
    public class FileUpload : CoreEntity
    {
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string CloudinaryUrl { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
    }

    #region Many-to-Many Relationships
    public class DocumentFileUpload
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileUploadId { get; set; } = string.Empty;

        public virtual Document? Document { get; set; }
        public virtual FileUpload? FileUpload { get; set; }
    }
    #endregion
}