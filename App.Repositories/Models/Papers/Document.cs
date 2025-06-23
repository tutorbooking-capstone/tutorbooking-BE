using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models.Papers
{
    public class Document : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string? StaffId { get; set; } = null; //Exist if the hardcopy document is uploaded by staff
        public string? HardcopySubmitId { get; set; } = null; //Exist if the hardcopy document is uploaded by hardcopy submit
        public string Description { get; set; } = string.Empty; //Description of document by Tutor
        public bool IsVisibleToLearner { get; set; } = false; // Whether the document is visible to learners
        public bool IsVerified { get; set; } = false;

        public virtual Staff? Staff { get; set; }
        public virtual TutorApplication? Application { get; set; }
        public virtual HardcopySubmit? HardcopySubmit { get; set; }
        public virtual ICollection<DocumentFileUpload> DocumentFileUploads { get; set; } = new List<DocumentFileUpload>();



        #region Behavior
        public Expression<Func<Document, object>>[] UpdateVisibility(bool isVisibleToLearner)
        {
            if (IsVisibleToLearner == isVisibleToLearner)
                return Array.Empty<Expression<Func<Document, object>>>();

            IsVisibleToLearner = isVisibleToLearner;
            return [x => x.IsVisibleToLearner];
        }

        public Expression<Func<Document, object>>[] UpdateDocumentInfo(
            string description,
            bool isVisibleToLearner)
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

            return modifiedProperties.ToArray();
        }
        #endregion
    }
}
