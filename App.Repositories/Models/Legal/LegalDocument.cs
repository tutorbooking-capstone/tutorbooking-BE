using App.Core.Base;
using System.Linq.Expressions;

namespace App.Repositories.Models.Legal
{
    public class LegalDocument : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public virtual ICollection<LegalDocumentVersion>? Versions { get; set; } 
        public virtual ICollection<LegalDocumentAcceptance>? LegalDocumentAcceptances { get; set; }

        #region Filter Expressions
        public static Expression<Func<LegalDocument, bool>> ActiveVersionExpression => 
            e => e.Versions.Any(v => v.Status == LegalDocumentStatus.Active);

        public static Expression<Func<LegalDocument, bool>> IsCategoryExpression(string category)
            => e => e.Category.ToLower().Equals(category.ToLower());
        
        public static Expression<Func<LegalDocument, bool>> UserNotAcceptedExpression(string userId)
            => e => !e.LegalDocumentAcceptances.Any(a => a.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
        #endregion
    }
}
