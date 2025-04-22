using FluentValidation;
using App.Repositories.Models;

namespace App.DTOs.HashtagDTOs
{
    public class UpdateTutorHashtagListRequest
    {
        // List of IDs of the hashtags the tutor wants to associate with their profile
        public List<string> HashtagIds { get; set; } = new List<string>();
    }

    #region Validator
    public class UpdateTutorHashtagListRequestValidator : AbstractValidator<UpdateTutorHashtagListRequest>
    {
        public UpdateTutorHashtagListRequestValidator()
        {
            RuleFor(x => x.HashtagIds)
                //.NotNull().WithMessage("Hashtag list cannot be null.")
                .Must(ids => ids.Count > 0).WithMessage("At least one hashtag must be selected.")
                .Must(ids => ids.Count <= 10).WithMessage("Cannot select more than 10 hashtags.");

            RuleForEach(x => x.HashtagIds)
                .NotEmpty().WithMessage("Hashtag ID cannot be empty.");
        }
    }
    #endregion

    #region Mapping
    public static class UpdateTutorHashtagListRequestExtensions
    {
        public static List<TutorHashtag> ToTutorHashtagEntities(
            this UpdateTutorHashtagListRequest request, 
            string tutorId)
        {
            return request.HashtagIds
                .Distinct()
                .Select(hashtagId => new TutorHashtag
                {
                    TutorId = tutorId,
                    HashtagId = hashtagId
                })
                .ToList();
        }
    }
    #endregion
}
