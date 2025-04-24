using App.Repositories.Models;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.Services.Interfaces;
using App.Core.Base;
using System.Linq.Expressions;

namespace App.Services.Services
{
    public class SeedService : ISeedService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public SeedService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        private async Task<List<T>> SeedResourceAsync<T>(
            string resourceName,
            List<T> seedData,
            Expression<Func<IUnitOfWork, IGenericRepository<T>>> repoSelector) where T : class
        {
            var repo = repoSelector.Compile()(_unitOfWork);
            
            try
            {
                repo.InsertRange(seedData);
                await _unitOfWork.SaveAsync();
                return seedData;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true)
            {
                throw new AlreadySeededException(
                    resourceName: resourceName,
                    seededData: seedData
                );
            }
        }

        public async Task<List<Hashtag>> SeedHashtagsAsync()
        {
            var hashtags = HashtagSeeder.SeedHashtags();
            return await SeedResourceAsync(
                resourceName: "Hashtags",
                seedData: hashtags,
                repoSelector: uow => uow.GetRepository<Hashtag>()
            );
        }

    }
} 