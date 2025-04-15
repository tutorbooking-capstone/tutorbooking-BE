using App.Repositories.Models;

namespace App.DTOs.BlogDTOs
{
    public class CreateBlogRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public static class CreateBlogRequestExtensions
    {
        public static Blog ToEntity(this CreateBlogRequest model, string userCreate)
        {
            return new Blog
            {
                Title = model.Title,
                Content = model.Content,
                UserId = userCreate,
            };
        }
    }
}
