using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface ISeedService
    {
        Task<List<Hashtag>> SeedHashtagsAsync();
    }
}