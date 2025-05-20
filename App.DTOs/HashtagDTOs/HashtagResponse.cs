using App.Repositories.Models;
using System.Linq.Expressions;

namespace App.DTOs.HashtagDTOs
{
    public class HashtagResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UsageCount { get; set; } = 0;

        public static Expression<Func<Hashtag, HashtagResponse>> ProjectionExpression
        => h => new HashtagResponse
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                UsageCount = h.UsageCount
            };
    }
}

