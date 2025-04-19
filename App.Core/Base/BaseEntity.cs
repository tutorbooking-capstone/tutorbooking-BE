using App.Core.Utils;
using System.ComponentModel.DataAnnotations;

namespace App.Core.Base
{
    public abstract class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedTime = LastUpdatedTime = CoreHelper.SystemTimeNow;
        }

        [Key]
        public string Id { get; set; }
        public string? CreatedBy { get; protected set; }
        public string? LastUpdatedBy { get; protected set; }
        public string? DeletedBy { get; protected set; }
        public DateTimeOffset CreatedTime { get; protected set; }
        public DateTimeOffset LastUpdatedTime { get; protected set; }
        public DateTimeOffset? DeletedTime { get; protected set; }

        #region Behavior
        public virtual void TrackCreate(string userId)
        {
            CreatedBy = userId;
            LastUpdatedBy = userId;
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }

        public virtual void TrackUpdate(string userId)
        {
            LastUpdatedBy = userId;
            LastUpdatedTime = CoreHelper.SystemTimeNow;
        }

        public virtual void TrackDelete(string userId)
        {
            DeletedBy = userId;
            DeletedTime = CoreHelper.SystemTimeNow;
            TrackUpdate(userId); // Update LastUpdated fields
        }
        #endregion
    }
}
