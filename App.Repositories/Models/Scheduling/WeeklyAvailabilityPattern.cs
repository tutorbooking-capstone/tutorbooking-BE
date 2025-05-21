using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models.Scheduling
{
    public class WeeklyAvailabilityPattern : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }   // Date from which this pattern applies (e.g., start of a week)

        public virtual Tutor? Tutor { get; set; }
        public virtual ICollection<AvailabilitySlot>? Slots { get; set; }

        #region Behavior
        public static WeeklyAvailabilityPattern? GetLatestPattern(ICollection<WeeklyAvailabilityPattern> patterns)
        {
            if (patterns == null || !patterns.Any())
                return null;

            return patterns
                .OrderByDescending(p => p.AppliedFrom)
                .FirstOrDefault();
        }
        #endregion
    }
}
