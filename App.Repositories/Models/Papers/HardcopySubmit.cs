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
        [EnumDescription("Đang chờ xử lý")]
        Pending = 0,
        
        [EnumDescription("Đang xử lý")]
        Processing = 1,
        
        [EnumDescription("Đã xác minh")]
        Verified = 2,
        
        [EnumDescription("Đã từ chối")]
        Rejected = 3
    }
}
