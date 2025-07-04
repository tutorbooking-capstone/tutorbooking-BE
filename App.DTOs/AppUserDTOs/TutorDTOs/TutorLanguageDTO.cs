﻿using System.Linq.Expressions;
using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorLanguageDTO
    {
        public string LanguageCode { get; set; } = string.Empty;
        public int Proficiency { get; set; }
        public bool IsPrimary { get; set; }

        public static Expression<Func<TutorLanguage, TutorLanguageDTO>> ProjectionExpression => tl 
        => new TutorLanguageDTO
            {
                LanguageCode = tl.LanguageCode,
                IsPrimary = tl.IsPrimary,
                Proficiency = tl.Proficiency
            };
    }

    #region Validator
    public class TutorLanguageDTOValidator : AbstractValidator<TutorLanguageDTO>
    {
        public TutorLanguageDTOValidator()
        {
            RuleFor(x => x.LanguageCode)
                .NotEmpty().WithMessage("Language code is required.")
                .Length(2, 5).WithMessage("Language code must be between 2 and 5 characters."); // Basic check for ISO codes

            RuleFor(x => x.Proficiency)
                .InclusiveBetween(1, 7).WithMessage("Proficiency must be between 1 and 7.");
        }
    }

    #endregion

    #region Mapping
    public static class TutorLanguageDTOExtensions
    {
        public static TutorLanguageDTO ToDTO(this TutorLanguage tutorLanguage)
        {
            return new TutorLanguageDTO
            {
                LanguageCode = tutorLanguage.LanguageCode,
                Proficiency = tutorLanguage.Proficiency,
                IsPrimary = tutorLanguage.IsPrimary
            };
        }

        public static List<TutorLanguageDTO> ToDTOs(this IEnumerable<TutorLanguage> tutorLanguages)
        {
            return tutorLanguages.Select(tl => tl.ToDTO()).ToList();
        }

        public static TutorLanguage ToEntity(this TutorLanguageDTO dto, string tutorId)
        {
            return new TutorLanguage
            {
                TutorId = tutorId,
                LanguageCode = dto.LanguageCode,
                Proficiency = dto.Proficiency,
                IsPrimary = dto.IsPrimary
            };
        }
    }
    #endregion
}
