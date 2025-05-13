using App.Repositories.Models;

namespace App.DTOs.HashtagDTOs
{
    public record HashtagResponse(
        string Id = "",
        string Name = "",
        string Description = "",
        int UsageCount = 0
    );

    #region Mapping
    public static class HashtagResponseExtensions
    {
        public static HashtagResponse ToHashtagResponse(this Hashtag entity)
        {
            return new HashtagResponse(
                Id: entity.Id,
                Name: entity.Name,
                Description: entity.Description,
                UsageCount: entity.UsageCount
            );
        }

        public static List<HashtagResponse> ToHashtagResponseList(this IEnumerable<Hashtag> entities)
        {
            return entities.Select(e => e.ToHashtagResponse()).ToList();
        }
    }
    #endregion
}
