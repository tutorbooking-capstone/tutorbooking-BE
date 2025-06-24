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
		//public VerificationStatus VerificationStatus { get; set; }
        public DateTime? BecameTutorAt { get; set; }
        
        // Scheduling information
        public List<WeeklyAvailabilityDTO> AvailabilityPatterns { get; set; } = new List<WeeklyAvailabilityDTO>();
        public List<BookingSlotDTO> BookingSlots { get; set; } = new List<BookingSlotDTO>();
        
        // Hashtags and Languages
        public List<HashtagDTO> Hashtags { get; set; } = new List<HashtagDTO>();
        public List<TutorLanguageDTO> Languages { get; set; } = new List<TutorLanguageDTO>();

        public static Expression<Func<Tutor, TutorResponse>> ProjectionExpression => t => new TutorResponse
        {
            UserId = t.UserId,
            Email = t.User == null ? string.Empty : t.User.Email ?? string.Empty,
            FullName = t.User == null ? string.Empty : t.User.FullName ?? string.Empty,
            NickName = t.NickName,
            Brief = t.Brief,
            Description = t.Description,
            TeachingMethod = t.TeachingMethod,
            //VerificationStatus = t.Languages.Any(),
            BecameTutorAt = t.BecameTutorAt
        };
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
    }

    public class WeeklyAvailabilityDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AppliedFrom { get; set; }
        public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();

        public static Expression<Func<WeeklyAvailabilityPattern, WeeklyAvailabilityDTO>> ProjectionExpression => 
            p => new WeeklyAvailabilityDTO
            {
                Id = p.Id,
                AppliedFrom = p.AppliedFrom,
                Slots = p.Slots != null 
                    ? p.Slots.Select(s => new AvailabilitySlotDTO
                    {
                        Id = s.Id,
                        Type = s.Type,
                        DayInWeek = s.DayInWeek,
                        SlotIndex = s.SlotIndex
                    }).ToList()
                    : new List<AvailabilitySlotDTO>()
            };
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
        
        public List<BookedSlotDTO> BookedSlots { get; set; } = new List<BookedSlotDTO>();

        public static Expression<Func<BookingSlot, BookingSlotDTO>> ProjectionExpression => 
        b => new BookingSlotDTO
        {
            Id = b.Id,
            LearnerId = b.LearnerId,
            Note = b.Note,
            StartDate = DateTime.MaxValue,
            RepeatForWeeks = 0,
            BookedSlots = b.BookedSlots != null 
            ? b.BookedSlots.Select(bs => new BookedSlotDTO
            {
                Id = bs.Id,
                BookedDate = bs.BookedDate,
                SlotNote = bs.SlotNote,
                Status = bs.Status,
                AvailabilitySlotId = bs.AvailabilitySlotId
            }).ToList() 
            : new List<BookedSlotDTO>()
        };
    }

    public class BookedSlotDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public string? SlotNote { get; set; }
        public SlotStatus Status { get; set; }
        public string AvailabilitySlotId { get; set; } = string.Empty;
    }

    #region Extension Methods
    public static class TutorResponseExtensions
    {
        public static TutorResponse ToTutorResponse(this Tutor tutor)
        {
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
                //VerificationStatus = tutor.VerificationStatus,
                BecameTutorAt = tutor.BecameTutorAt
            };
        }
    }
    #endregion
}
