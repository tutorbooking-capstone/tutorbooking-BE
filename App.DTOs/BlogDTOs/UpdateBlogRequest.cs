using App.Repositories.Models;

namespace App.DTOs.BlogDTOs
{
    public class UpdateBlogRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int? LikeCount { get; set; }
    }

    public static class UpdateBlogRequestExtensions
    {
        public static void ApplyUpdateModel(this Blog entity, UpdateBlogRequest model)
        {
            if (!string.IsNullOrEmpty(model.Title))
                entity.Title = model.Title;

            if (!string.IsNullOrEmpty(model.Content))
                entity.Content = model.Content;

            if (model.LikeCount.HasValue)
                entity.LikeCount = model.LikeCount.Value;
        }
    }
}
