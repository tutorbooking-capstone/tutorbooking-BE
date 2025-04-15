using App.Repositories.Models;

namespace App.DTOs.BlogDTOs
{
    public class BlogResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public static class BlogResponseExtensions
    {
        public static BlogResponse ToBlogResponse(this Blog entity)
        {
            return new BlogResponse
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
                LikeCount = entity.LikeCount,
                UserId = entity.UserId ?? "SystemCreate",
                FullName = entity.AppUser?.FullName ?? "System404"
            };
        }
    }
}
