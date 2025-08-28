using App.Repositories.Models;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorCardDTO
    {
        public string TutorId { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsProfessional { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; } = 0;
        public string IntroductionVideoUrl { get; set; } = string.Empty;
        public List<TutorCardLanguageDTO> Languages { get; set; } = new List<TutorCardLanguageDTO>();
        public List<DailyAvailabilityPatternDTO> AvailabilityPatterns { get; set; } = new();
        public List<TutorHashtagDTO> Hashtags { get; set; } = new List<TutorHashtagDTO>();


        public static Expression<Func<Tutor, TutorCardDTO>> Projection = t => new TutorCardDTO()
        {
            TutorId = t.UserId,
            ProfileImageUrl = t.User.ProfilePictureUrl,
            FullName = t.User.FullName,
            NickName = t.NickName,
            Description = t.Description,
            IsProfessional = t.BookingSlotRatings.Count >= 50
                                        &&
                                        t.BookingSlotRatings
                                        .Select(e => (e.TeachingQuality + e.Attitude + e.Commitment) / 3)
                                        .DefaultIfEmpty()
                                        .Average() >= 4.5,
            Rating = t.BookingSlotRatings
                                .Select(e => (e.TeachingQuality + e.Attitude + e.Commitment) / 3)
                                .DefaultIfEmpty()
                                .Average(),
            TotalReviews = t.BookingSlotRatings.Count,
            Languages = t.Languages.OrderByDescending(l => l.IsPrimary)
                                    .ThenByDescending(l => l.Proficiency)
                                    .Select(l => new TutorCardLanguageDTO
                                    {
                                        LanguageCode = l.LanguageCode,
                                        IsPrimary = l.IsPrimary,
                                        Proficiency = l.Proficiency
                                    })
                                    .ToList(),
            AvailabilityPatterns = t.AvailabilityPatterns.
                                                OrderByDescending(a => a.AppliedFrom)
                                                .Take(1)
                                                .SelectMany(a => a.Slots
                                                                .GroupBy(slot => slot.DayInWeek)
                                                                .Select(group => new DailyAvailabilityPatternDTO
                                                                {
                                                                    Day = group.Key,
                                                                    Date = a.AppliedFrom.AddDays((int)group.Key),
                                                                    TimeSlotIndex = group.Select(slot => slot.SlotIndex).OrderBy(x => x).ToList()
                                                                }))
                                                .ToList(),
            IntroductionVideoUrl = t.IntroductionVideos
                                                .Where(iv => iv.Status == TutorIntroductionVideoStatus.Active)
                                                .Select(iv => iv.Url)
                                                .FirstOrDefault() ?? string.Empty,
            Hashtags = t.Hashtags
                                    .Select(th => new TutorHashtagDTO
                                    {
                                        HashtagId = th.HashtagId,
                                        Name = th.Hashtag.Name
                                    }).ToList()
        };

        public static Expression<Func<Tutor, TutorCardDTO>> SimpleProjection = t => new TutorCardDTO()
        {
            TutorId = t.UserId,
            ProfileImageUrl = t.User.ProfilePictureUrl,
            FullName = t.User.FullName,
            NickName = t.NickName,
            Description = t.Description,
            IsProfessional = t.BookingSlotRatings.Count >= 50
                                        &&
                                        t.BookingSlotRatings
                                        .Select(e => (e.TeachingQuality + e.Attitude + e.Commitment) / 3)
                                        .DefaultIfEmpty()
                                        .Average() >= 4.5,
            Rating = t.BookingSlotRatings
                                .Select(e => (e.TeachingQuality + e.Attitude + e.Commitment) / 3)
                                .DefaultIfEmpty()
                                .Average(),
            TotalReviews = t.BookingSlotRatings.Count,
            Languages = t.Languages.OrderByDescending(l => l.IsPrimary)
                                    .ThenByDescending(l => l.Proficiency)
                                    .Select(l => new TutorCardLanguageDTO
                                    {
                                        LanguageCode = l.LanguageCode,
                                        IsPrimary = l.IsPrimary,
                                        Proficiency = l.Proficiency
                                    })
                                    .ToList(),
            IntroductionVideoUrl = t.IntroductionVideos
                                                .Where(iv => iv.Status == TutorIntroductionVideoStatus.Active)
                                                .Select(iv => iv.Url)
                                                .FirstOrDefault() ?? string.Empty
        };

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
            double rating = 0.0,
            string introductionVideoUrl = "")
        {
            return new TutorCardDTO
            {
                TutorId = tutor.UserId,
                ProfileImageUrl = tutor.User?.ProfilePictureUrl ?? string.Empty,
                FullName = tutor.User?.FullName ?? string.Empty,
                NickName = tutor.NickName,
				Description = tutor.Description,
                //IsProfessional = tutor.VerificationStatus == VerificationStatus.VerifiedHardcopy,
                IsProfessional = tutor.Languages.Any(),
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
                    .ToList(),
                IntroductionVideoUrl = introductionVideoUrl
            };
        }
    }
    #endregion
}