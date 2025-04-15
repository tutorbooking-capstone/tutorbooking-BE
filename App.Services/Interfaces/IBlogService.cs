using App.Core.Base;
using App.DTOs.BlogDTOs;

namespace App.Services.Interfaces
{
    public interface IBlogService
    {
        Task<IEnumerable<BlogResponse>> GetAllAsync();
        Task<BlogResponse> GetByIdAsync(string id);
        Task CreateAsync(CreateBlogRequest model);
        Task UpdateAsync(string id, UpdateBlogRequest model);
        Task DeleteAsync(string id);
        Task<BasePaginatedList<BlogResponse>> GetPageBlogsAsync(int index, int pageSize);
    }
}
