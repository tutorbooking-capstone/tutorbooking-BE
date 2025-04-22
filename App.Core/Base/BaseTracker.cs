using App.Core.Utils;

namespace App.Core.Base
{
    public interface ITrackable
    {
        string? CreatedBy { get; set; }
        string? LastUpdatedBy { get; set; }
        string? DeletedBy { get; set; }
        DateTimeOffset CreatedTime { get; set; }
        DateTimeOffset LastUpdatedTime { get; set; }
        DateTimeOffset? DeletedTime { get; set; }
    }

    public static class TrackableExtensions
    {
        public static void TrackCreate(this ITrackable entity, string userId)
        {
            entity.CreatedBy = userId;
            entity.LastUpdatedBy = userId;
            entity.CreatedTime = CoreHelper.SystemTimeNow;
            entity.LastUpdatedTime = entity.CreatedTime;
        }

        public static void TrackUpdate(this ITrackable entity, string userId)
        {
            entity.LastUpdatedBy = userId;
            entity.LastUpdatedTime = CoreHelper.SystemTimeNow;
        }

        public static void TrackDelete(this ITrackable entity, string userId)
        {
            entity.DeletedBy = userId;
            entity.DeletedTime = CoreHelper.SystemTimeNow;
            entity.TrackUpdate(userId);
        }
    }
}
