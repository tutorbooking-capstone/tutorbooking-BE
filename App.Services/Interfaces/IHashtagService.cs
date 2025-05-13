using App.DTOs.HashtagDTOs;

namespace App.Services.Interfaces
{
    public interface IHashtagService
    {
        Task<List<HashtagResponse>> GetAllHashtagsAsync();
        // List<Hashtag> GetSeedHashtags();
        // Task SeedHashtagsAsync();
    }
}