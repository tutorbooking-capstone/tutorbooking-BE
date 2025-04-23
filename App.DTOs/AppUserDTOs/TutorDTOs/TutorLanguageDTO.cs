using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorLanguageDTO
    {
        public string LanguageCode { get; set; } = string.Empty;
        public int Proficiency { get; set; }
        public bool IsPrimary { get; set; }
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

    //public class TutorLanguageListValidator : AbstractValidator<List<TutorLanguageDTO>>
    //{
    //    public TutorLanguageListValidator(IValidator<TutorLanguageDTO> itemValidator)
    //    {
    //        RuleForEach(x => x).SetValidator(itemValidator);
    //    }
    //}
    #endregion

    #region Mapping
    public static class TutorLanguageDTOExtensions
    {
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
