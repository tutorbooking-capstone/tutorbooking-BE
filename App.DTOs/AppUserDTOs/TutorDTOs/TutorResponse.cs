using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.Models.Scheduling;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorResponse
    {  
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TeachingMethod { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
        public DateTime? BecameTutorAt { get; set; }
        
        // Scheduling information
        public List<WeeklyAvailabilityDTO> AvailabilityPatterns { get; set; } = new List<WeeklyAvailabilityDTO>();
        public List<BookingSlotDTO> BookingSlots { get; set; } = new List<BookingSlotDTO>();
        
        // Hashtags and Languages
        public List<HashtagDTO> Hashtags { get; set; } = new List<HashtagDTO>();
        public List<TutorLanguageDTO> Languages { get; set; } = new List<TutorLanguageDTO>();
    }

    public class HashtagDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }


    public class WeeklyAvailabilityDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();
    }

    public class AvailabilitySlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public SlotType Type { get; set; }
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
        public string? BookingSlotId { get; set; }
    }

    public class BookingSlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public string? LearnerId { get; set; }
        public string? Note { get; set; }
        public DateTime StartDate { get; set; }
        public int? RepeatForWeeks { get; set; }
        public List<string> AssociatedSlotIds { get; set; } = new List<string>();
    }

    #region Mapping
    public static class TutorResponseExtensions
    {
        public static TutorResponse ToTutorResponse(this Tutor tutor)
        {
            return new TutorResponse
            {
                UserId = tutor.UserId,
                Email = tutor.User?.Email ?? "N/A", 
                FullName = tutor.User?.FullName ?? "N/A", 
                NickName = tutor.NickName,
                Brief = tutor.Brief,
                Description = tutor.Description,
                TeachingMethod = tutor.TeachingMethod,
                VerificationStatus = tutor.VerificationStatus,
                BecameTutorAt = tutor.BecameTutorAt
            };
        }

        public static TutorResponse ToDetailedTutorResponse(
            this Tutor tutor, 
            ICollection<WeeklyAvailabilityPattern>? patterns = null,
            ICollection<BookingSlot>? bookings = null,
            ICollection<TutorHashtag>? tutorHashtags = null,
            ICollection<TutorLanguage>? tutorLanguages = null)
        {
            var response = tutor.ToTutorResponse();

            if (patterns != null)
            {
                response.AvailabilityPatterns = patterns.Select(p => new WeeklyAvailabilityDTO
                {
                    Id = p.Id,
                    AppliedFrom = p.AppliedFrom,
                    Slots = p.Slots?.Select(s => new AvailabilitySlotDTO
                    {
                        Id = s.Id,
                        Type = s.Type,
                        DayInWeek = s.DayInWeek,
                        SlotIndex = s.SlotIndex,
                        BookingSlotId = s.BookingSlotId
                    }).ToList() ?? new List<AvailabilitySlotDTO>()
                }).ToList();
            }

            if (bookings != null)
            {
                response.BookingSlots = bookings.Select(b => new BookingSlotDTO
                {
                    Id = b.Id,
                    LearnerId = b.LearnerId,
                    Note = b.Note,
                    StartDate = b.StartDate,
                    RepeatForWeeks = b.RepeatForWeeks,
                    AssociatedSlotIds = b.Slots?.Select(s => s.Id).ToList() ?? new List<string>()
                }).ToList();
            }

            if (tutorHashtags != null)
            {
                response.Hashtags = tutorHashtags
                    .Where(th => th.Hashtag != null)
                    .Select(th => new HashtagDTO
                    {
                        Id = th.HashtagId,
                        Name = th.Hashtag?.Name ?? string.Empty,
                        Description = th.Hashtag?.Description ?? string.Empty
                    }).ToList();
            }

            if (tutorLanguages != null)
            {
                response.Languages = tutorLanguages.Select(tl => new TutorLanguageDTO
                {
                    LanguageCode = tl.LanguageCode,
                    IsPrimary = tl.IsPrimary,
                    Proficiency = tl.Proficiency
                }).ToList();
            }

            return response;
        }
    }
    #endregion
}
