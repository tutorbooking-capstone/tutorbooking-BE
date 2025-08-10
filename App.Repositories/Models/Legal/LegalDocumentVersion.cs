using App.Core.Base;

namespace App.Repositories.Models.Legal
{
    public class LegalDocumentVersion : BaseEntity
    {
        public string LegalDocumentId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public LegalDocumentStatus Status { get; set; } = LegalDocumentStatus.Draft;
        public string Content { get; set; } = string.Empty;
        public string ContentType {  get; set; } = string.Empty;
        public virtual LegalDocument? LegalDocument { get; set; }
        public virtual ICollection<LegalDocumentAcceptance>? LegalDocumentAcceptances { get; set; }
    }

    public enum LegalDocumentStatus
    {
        Draft,
        Inactive,
        Active
    }
}
