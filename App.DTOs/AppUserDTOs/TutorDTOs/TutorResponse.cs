using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

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
		public string ProfileImageUrl  { get; set; } = string.Empty;
		public VerificationStatus VerificationStatus { get; set; }
        public DateTime? BecameTutorAt { get; set; }
        
        // Scheduling information
        public List<WeeklyAvailabilityDTO> AvailabilityPatterns { get; set; } = new List<WeeklyAvailabilityDTO>();
        public List<BookingSlotDTO> BookingSlots { get; set; } = new List<BookingSlotDTO>();
        
        // Hashtags and Languages
        public List<HashtagDTO> Hashtags { get; set; } = new List<HashtagDTO>();
        public List<TutorLanguageDTO> Languages { get; set; } = new List<TutorLanguageDTO>();

        // Phương thức ánh xạ trực tiếp, thay thế RegisterMappings
        public static Expression<Func<Tutor, TutorResponse>> ProjectionExpression => t => new TutorResponse
        {
            UserId = t.UserId,
            Email = t.User == null ? string.Empty : t.User.Email ?? string.Empty,
            FullName = t.User == null ? string.Empty : t.User.FullName ?? string.Empty,
            NickName = t.NickName,
            Brief = t.Brief,
            Description = t.Description,
            TeachingMethod = t.TeachingMethod,
            VerificationStatus = t.VerificationStatus,
            BecameTutorAt = t.BecameTutorAt
        };

        public TutorResponse WithRelatedData(
            List<HashtagDTO> hashtags,
            List<TutorLanguageDTO> languages,
            ICollection<WeeklyAvailabilityPattern> patterns,
            ICollection<BookingSlot> bookings)
        {
            this.Hashtags = hashtags;
            this.Languages = languages;
            
            this.AvailabilityPatterns = patterns.Select(WeeklyAvailabilityDTO.FromEntity).ToList();
            this.BookingSlots = bookings.Select(BookingSlotDTO.FromEntity).ToList();
            
            return this;
        }
    }

    public class HashtagDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public static Expression<Func<TutorHashtag, HashtagDTO>> ProjectionExpression => th => new HashtagDTO
        {
            Id = th.HashtagId,
            Name = th.Hashtag != null ? th.Hashtag.Name : string.Empty,
            Description = th.Hashtag != null ? th.Hashtag.Description : string.Empty
        };

        public static HashtagDTO FromEntity(TutorHashtag entity)
        {
            return new HashtagDTO
            {
                Id = entity.HashtagId,
                Name = entity.Hashtag?.Name ?? string.Empty,
                Description = entity.Hashtag?.Description ?? string.Empty
            };
        }
    }

    public class WeeklyAvailabilityDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();

        public static Expression<Func<WeeklyAvailabilityPattern, WeeklyAvailabilityDTO>> ProjectionExpression => p => new WeeklyAvailabilityDTO
        {
            Id = p.Id,
            AppliedFrom = p.AppliedFrom
        };

        public static WeeklyAvailabilityDTO FromEntity(WeeklyAvailabilityPattern entity)
        {
            var dto = new WeeklyAvailabilityDTO
            {
                Id = entity.Id,
                AppliedFrom = entity.AppliedFrom
            };

            if (entity.Slots != null)
                dto.Slots = entity.Slots.Select(AvailabilitySlotDTO.FromEntity).ToList();

            return dto;
        }
    }

    public class AvailabilitySlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public SlotType Type { get; set; }
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
        public string? BookingSlotId { get; set; }

        public static Expression<Func<AvailabilitySlot, AvailabilitySlotDTO>> ProjectionExpression => s => new AvailabilitySlotDTO
        {
            Id = s.Id,
            Type = s.Type,
            DayInWeek = s.DayInWeek,
            SlotIndex = s.SlotIndex,
            BookingSlotId = s.BookingSlotId
        };

        public static AvailabilitySlotDTO FromEntity(AvailabilitySlot entity)
        {
            return new AvailabilitySlotDTO
            {
                Id = entity.Id,
                Type = entity.Type,
                DayInWeek = entity.DayInWeek,
                SlotIndex = entity.SlotIndex,
                BookingSlotId = entity.BookingSlotId
            };
        }
    }

    public class BookingSlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public string? LearnerId { get; set; }
        public string? Note { get; set; }
        public DateTime StartDate { get; set; }
        public int? RepeatForWeeks { get; set; }
        public List<string> AssociatedSlotIds { get; set; } = new List<string>();

        public static Expression<Func<BookingSlot, BookingSlotDTO>> ProjectionExpression => b => new BookingSlotDTO
        {
            Id = b.Id,
            LearnerId = b.LearnerId,
            Note = b.Note,
            StartDate = b.StartDate,
            RepeatForWeeks = b.RepeatForWeeks
        };

        public static BookingSlotDTO FromEntity(BookingSlot entity)
        {
            var dto = new BookingSlotDTO
            {
                Id = entity.Id,
                LearnerId = entity.LearnerId,
                Note = entity.Note,
                StartDate = entity.StartDate,
                RepeatForWeeks = entity.RepeatForWeeks
            };

            if (entity.Slots != null)
                dto.AssociatedSlotIds = entity.Slots.Select(s => s.Id).ToList();

            return dto;
        }
    }

    #region Extension Methods
    public static class TutorResponseExtensions
    {
        public static TutorResponse ToTutorResponse(this Tutor tutor)
        {
            // Sử dụng phương thức ánh xạ trực tiếp thay vì MapTo
            return new TutorResponse
            {
                UserId = tutor.UserId,
                Email = tutor.User == null ? string.Empty : tutor.User.Email ?? string.Empty,
                FullName = tutor.User == null ? string.Empty : tutor.User.FullName ?? string.Empty,
                NickName = tutor.NickName,
                Brief = tutor.Brief,
                Description = tutor.Description,
                TeachingMethod = tutor.TeachingMethod,
				ProfileImageUrl = tutor.User != null ? tutor.User.ProfilePictureUrl : string.Empty,
                VerificationStatus = tutor.VerificationStatus,
                BecameTutorAt = tutor.BecameTutorAt
            };
        }

        // public static TutorResponse ToDetailedTutorResponse(
        //     this Tutor tutor, 
        //     ICollection<WeeklyAvailabilityPattern>? patterns = null,
        //     ICollection<BookingSlot>? bookings = null,
        //     ICollection<TutorHashtag>? tutorHashtags = null,
        //     ICollection<TutorLanguage>? tutorLanguages = null)
        // {
        //     var response = tutor.ToTutorResponse();

        //     if (patterns != null)
        //     {
        //         response.AvailabilityPatterns = patterns.Select(WeeklyAvailabilityDTO.FromEntity).ToList();
        //     }

        //     if (bookings != null)
        //     {
        //         response.BookingSlots = bookings.Select(BookingSlotDTO.FromEntity).ToList();
        //     }

        //     if (tutorHashtags != null)
        //     {
        //         response.Hashtags = tutorHashtags
        //             .Where(th => th.Hashtag != null)
        //             .Select(HashtagDTO.FromEntity).ToList();
        //     }

        //     if (tutorLanguages != null)
        //     {
        //         response.Languages = tutorLanguages.Select(TutorLanguageDTO.FromEntity).ToList();
        //     }

        //     return response;
        // }
    }
    #endregion
}
