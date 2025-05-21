using App.Repositories.Models;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorHashtagDTO
    {
        public string HashtagId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #region Mapping
    public static class TutorHashtagDTOExtensions
    {
        public static TutorHashtagDTO ToDTO(this TutorHashtag tutorHashtag)
        {
            return new TutorHashtagDTO
            {
                HashtagId = tutorHashtag.HashtagId,
                Name = tutorHashtag.Hashtag?.Name ?? string.Empty
            };
        }

        public static List<TutorHashtagDTO> ToDTOs(this IEnumerable<TutorHashtag> tutorHashtags)
        {
            return tutorHashtags.Select(th => th.ToDTO()).ToList();
        }
    }
    #endregion
}
