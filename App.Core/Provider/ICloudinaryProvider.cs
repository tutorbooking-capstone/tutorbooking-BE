using Microsoft.AspNetCore.Http;

namespace App.Core.Provider
{
    public interface ICloudinaryProvider
    {
        Task<string> UploadDocumentAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string publicId);
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string? existingPublicId = null);
    }
}
