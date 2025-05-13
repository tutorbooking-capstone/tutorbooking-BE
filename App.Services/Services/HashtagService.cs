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
        // public async Task SeedHashtagsAsync()
        // {
        //     var repo = _unitOfWork.GetRepository<Hashtag>();
        //     var existingCount = await repo.ExistEntities().CountAsync();
            
        //     if (existingCount > 0) return;

        //     var hashtags = HashtagSeeder.SeedHashtags();
        //     repo.InsertRange(hashtags);
        //     await _unitOfWork.SaveAsync();
        // }

        // public List<Hashtag> GetSeedHashtags()
        //     => HashtagSeeder.SeedHashtags();
        #endregion

        public async Task<List<HashtagResponse>> GetAllHashtagsAsync()
        {
            var hashtags = await _unitOfWork.GetRepository<Hashtag>()
                .ExistEntities()
                .ToListAsync();

            return hashtags.ToHashtagResponseList();
        }
    }
} 