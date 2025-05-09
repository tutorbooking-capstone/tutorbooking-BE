using App.Core.Base;

namespace App.Repositories.Models.Papers
{
    public class HardcopySubmit : BaseEntity
    {
        public string ApplicationId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public HardcopySubmitStatus Status { get; set; } = HardcopySubmitStatus.Pending;
        public string StaffNotes { get; set; } = string.Empty;

        public virtual TutorApplication? Application { get; set; }
        public virtual ICollection<Document>? Documents { get; set; } 
    }

    public enum HardcopySubmitStatus
    {
        Pending = 0,       
        Verified = 1,     
        Rejected = 2      
    }
}
