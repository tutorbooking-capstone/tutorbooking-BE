using App.DTOs.HashtagDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.Services.Interfaces;

namespace App.Services.Services
{
    public class HashtagService : IHashtagService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public HashtagService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Seed Hashtags
        public async Task SeedHashtagsAsync()
        {
            var repo = _unitOfWork.GetRepository<Hashtag>();
            if (await repo.ExistEntities().AnyAsync()) return;

            repo.InsertRange(HashtagSeeder.SeedHashtags());
            await _unitOfWork.SaveAsync();
        }

        public List<Hashtag> GetSeedHashtags()
            => HashtagSeeder.SeedHashtags();
        #endregion

        public async Task<List<HashtagResponse>> GetAllHashtagsAsync()
        {
            return await _unitOfWork.GetRepository<Hashtag>()
                .ExistEntities()
                .Select(HashtagResponse.ProjectionExpression)
                .ToListAsync();
        }
    }
} 