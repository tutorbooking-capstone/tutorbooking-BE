using App.Core.Base;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models
{
    public class TutorIntroductionVideo: BaseEntity
    {
        public string TutorUserId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public TutorIntroductionVideoStatus Status { get; set; } = TutorIntroductionVideoStatus.Pending;
        public virtual Tutor? Tutor { get; set; }
    }

    public enum TutorIntroductionVideoStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
