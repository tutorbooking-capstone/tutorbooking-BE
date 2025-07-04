﻿using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class TutorLanguage : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; //Base on ISO Code
        public bool IsPrimary { get; set; } //Is main teaching language or not
        public int Proficiency { get; set; } //Represent the level of proficiency in the language (1-7 scale)
        //public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual Tutor? Tutor { get; set; }
    }

    //ISO Code
    //https://www.w3schools.com/tags/ref_language_codes.asp
}
