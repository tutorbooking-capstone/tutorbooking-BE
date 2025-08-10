using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models.Legal
{
    public class LegalDocumentAcceptance : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string LegalDocumentId { get; set; } = string.Empty;
        public string LegalDocumentVersionId { get; set; } = string.Empty;
        public virtual AppUser? AppUser { get; set; }
        public virtual LegalDocument? LegalDocument { get; set; }
        public virtual LegalDocumentVersion? LegalDocumentVersion { get; set; }
    }
}
