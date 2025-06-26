using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

namespace App.Repositories.Models.User
{
    public class Learner
    {
        public string UserId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; //Base on ISO Code
        public int ProficiencyLevel { get; set; } = 1; //Represent the level of proficiency in the language (1-7 scale)

        public virtual AppUser? User { get; set; }
        public virtual ICollection<BookingSlot>? BookingSlots { get; set; }
        public virtual ICollection<LearnerTimeSlotRequest>? TimeSlotRequests { get; set; }

        #region Behavior
        public Expression<Func<Learner, object>>[] UpdateLearningLanguage(
            string? languageCode,
            int? proficiencyLevel)
        {
            var updatedFields = new List<Expression<Func<Learner, object>>>();

            if (languageCode != null && LanguageCode != languageCode)
            {
                LanguageCode = languageCode;
                updatedFields.Add(x => x.LanguageCode);
            }

            if (proficiencyLevel.HasValue && ProficiencyLevel != proficiencyLevel 
                && proficiencyLevel.Value >= 1 && proficiencyLevel.Value <= 7)
            {
                ProficiencyLevel = proficiencyLevel.Value;
                updatedFields.Add(x => x.ProficiencyLevel);
            }

            return updatedFields.ToArray();
        }
        #endregion
    }
}
