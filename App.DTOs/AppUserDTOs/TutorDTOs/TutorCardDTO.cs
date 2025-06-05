using App.Repositories.Models;
using App.Repositories.Models.User;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorCardDTO
    {
        public string TutorId { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
		public string Description {  get; set; } = string.Empty;
        public bool IsProfessional { get; set; }
        public double Rating { get; set; }
        public List<TutorCardLanguageDTO> Languages { get; set; } = new List<TutorCardLanguageDTO>();
    }

    public class TutorCardLanguageDTO
    {
        public string LanguageCode { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int Proficiency { get; set; }
    }

    #region Mapping
    public static class TutorCardDTOExtensions
    {
        public static TutorCardDTO ToTutorCardDTO(
            this Tutor tutor,
            List<TutorLanguage> languages,
            double rating = 0.0)
        {
            return new TutorCardDTO
            {
                TutorId = tutor.UserId,
                ProfileImageUrl = tutor.User?.ProfilePictureUrl ?? string.Empty,
                FullName = tutor.User?.FullName ?? string.Empty,
                NickName = tutor.NickName,
				Description = tutor.Description,
                IsProfessional = tutor.VerificationStatus == VerificationStatus.VerifiedHardcopy,
                Rating = rating,
                Languages = languages
                    .OrderByDescending(l => l.IsPrimary)
                    .ThenByDescending(l => l.Proficiency)
                    .Select(l => new TutorCardLanguageDTO
                    {
                        LanguageCode = l.LanguageCode,
                        IsPrimary = l.IsPrimary,
                        Proficiency = l.Proficiency
                    })
                    .ToList()
            };
        }
    }
    #endregion
}