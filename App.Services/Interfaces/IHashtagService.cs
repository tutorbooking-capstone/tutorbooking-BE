using App.DTOs.HashtagDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface IHashtagService
    {
        Task<List<HashtagResponse>> GetAllHashtagsAsync();
        List<Hashtag> GetSeedHashtags();
        Task SeedHashtagsAsync();
    }
}