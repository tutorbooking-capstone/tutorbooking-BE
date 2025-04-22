using App.Core.Base;
using App.Core.Utils;
using App.DTOs.BlogDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Infras;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class BlogService : IBlogService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;

        public BlogService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
        }
        #endregion

        public async Task<IEnumerable<BlogResponse>> GetAllAsync()
        {
            var query = _unitOfWork.GetRepository<Blog>().ExistEntities()
                .Include(b => b.AppUser)
                .OrderByDescending(b => b.CreatedTime);

            var blogs = await query.ToListAsync();
            return blogs.Select(blog => blog.ToBlogResponse());
        }

        public async Task<BlogResponse> GetByIdAsync(string id)
        {
            var blog = await _unitOfWork.GetRepository<Blog>().GetExistByIdAsync(id.Trim());
            return blog.ToBlogResponse();
        }

        public async Task CreateAsync(CreateBlogRequest model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var newBlog = model.ToEntity(userId);
            newBlog.TrackCreate(userId);

            _unitOfWork.GetRepository<Blog>().Insert(newBlog);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(string id, UpdateBlogRequest model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var blog = await _unitOfWork.GetRepository<Blog>().GetExistByIdAsync(id.Trim());
            blog.ApplyUpdateModel(model);
            blog.TrackUpdate(userId);

            _unitOfWork.GetRepository<Blog>().Insert(blog);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(string id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var blog = await _unitOfWork.GetRepository<Blog>().GetExistByIdAsync(id.Trim());

            blog.TrackDelete(userId);
            _unitOfWork.GetRepository<Blog>().Insert(blog);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<BlogResponse>> GetPageBlogsAsync(int index, int pageSize)
        {
            var query = _unitOfWork.GetRepository<Blog>().ExistEntities()
                .Include(b => b.AppUser)
                .OrderByDescending(b => b.LastUpdatedTime);

            var result = await _unitOfWork.GetRepository<Blog>().GetPagging(query, index, pageSize);
            
            return new BasePaginatedList<BlogResponse>(
                result.Items.Select(blog => blog.ToBlogResponse()).ToList(),
                result.TotalItems,
                result.PageIndex,
                result.PageSize
            );
        }
    }
}
