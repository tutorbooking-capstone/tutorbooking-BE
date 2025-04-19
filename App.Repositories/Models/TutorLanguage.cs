using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class TutorLanguage : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;

        //Base on ISO Code
        public string LanguageCode { get; set; } = string.Empty;

        //Is main language or not
        public bool IsPrimary { get; set; }

        //Represent the level of proficiency in the language (1-7 scale)
        public int Proficiency { get; set; }

        public virtual Tutor Tutor { get; set; } = new();
    }

    //ISO Code
    //https://www.w3schools.com/tags/ref_language_codes.asp
}
