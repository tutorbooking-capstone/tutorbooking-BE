using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.DocumentDTOs
{
    public class DocumentVerifyRequest
    {
        public string DocumentId { get; set; }
        public bool IsVerified { get; set; }
    }

    public class DocumentVerifyRequestList
    {
        public List<DocumentVerifyRequest> requests { get; set; }
    }
}
