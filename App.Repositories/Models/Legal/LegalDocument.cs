using App.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models.Legal
{
    public class LegalDocument : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public virtual ICollection<LegalDocumentVersion>? Versions { get; set; } 
        public virtual ICollection<LegalDocumentAcceptance>? LegalDocumentAcceptances { get; set; }
    }
}
