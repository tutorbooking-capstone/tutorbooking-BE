using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public class Document : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string? StaffId { get; set; } = null; //Exist if the hardcopy document is uploaded by staff
        public string Description { get; set; } = string.Empty; //Description of document by Tutor
        public bool IsVisibleToLearner { get; set; } = false; // Whether the document is visible to learners
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // 3rd archive
        public string CloudinaryUrl { get; set; } = string.Empty; // Public URL to access the file

        public virtual TutorApplication? Application { get; set; }
        public virtual Staff? Staff { get; set; }

        #region Behavior
        public Expression<Func<Document, object>>[] UpdateDocumentInfo(
            string description,
            bool isVisibleToLearner,
            string contentType,
            long fileSize,
            string cloudinaryUrl)
        {
            var modifiedProperties = new List<Expression<Func<Document, object>>>();

            if (Description != description)
            {
                Description = description;
                modifiedProperties.Add(x => x.Description);
            }

            if (IsVisibleToLearner != isVisibleToLearner)
            {
                IsVisibleToLearner = isVisibleToLearner;
                modifiedProperties.Add(x => x.IsVisibleToLearner);
            }

            if (ContentType != contentType)
            {
                ContentType = contentType;
                modifiedProperties.Add(x => x.ContentType);
            }

            if (FileSize != fileSize)
            {
                FileSize = fileSize;
                modifiedProperties.Add(x => x.FileSize);
            }

            if (CloudinaryUrl != cloudinaryUrl)
            {
                CloudinaryUrl = cloudinaryUrl;
                modifiedProperties.Add(x => x.CloudinaryUrl);
            }
                
            return modifiedProperties.ToArray();
        }
        #endregion
    }
}
