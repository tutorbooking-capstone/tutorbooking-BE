using App.Core.Utils;
using System.ComponentModel.DataAnnotations;

namespace App.Core.Base
{
    public abstract class CoreEntity
    {
        protected CoreEntity()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        [Key]
        public string Id { get; set; }
    }

    public abstract class BaseEntity : CoreEntity, ITrackable
    {
        protected BaseEntity()
        {
            CreatedTime = LastUpdatedTime = CoreHelper.SystemTimeNow;
        }

        public string? CreatedBy { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public DateTimeOffset? DeletedTime { get; set; }
    }
}