using App.Core.Base;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models
{
    public class MeritAdjustment: CoreEntity
    {
        public string UserId { get; set; } = string.Empty;
        public float Value { get; set; } = 0.0f;
        public float PreviousMeritScore { get; set; } = 0.0f;
        public float NewMeritScore { get; set; } = 0.0f;
        public string Reason { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        public virtual AppUser? User { get; set; }
    }
}
