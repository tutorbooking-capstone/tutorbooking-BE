using App.Core.Base;
using App.Core.Jsetting;
using App.Core.Provider;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace App.Services.Infras
{
    public class CloudinaryProvider : ICloudinaryProvider
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryProvider(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidArgumentException(
                    paramName: nameof(file),
                    message: "File không được null hoặc rỗng."
                );

            await using var stream = file.OpenReadStream();
            
            if (file.ContentType.StartsWith("video/"))
            {
                var uploadParams = new VideoUploadParams()  
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = Guid.NewGuid().ToString()
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
            else
            {
                var uploadParams = new RawUploadParams()  
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = Guid.NewGuid().ToString()
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString();
            }
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string? existingPublicId = null)
        {
            if (file == null || file.Length == 0)
                throw new InvalidArgumentException(
                    paramName: nameof(file),
                    message: "File không được null hoặc rỗng."
                );

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidArgumentException(
                    paramName: nameof(file),
                    message: "File không phải là định dạng ảnh hợp lệ."
                );

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = Guid.NewGuid().ToString(),
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    "cloudinary_upload_failed",
                    $"Cloudinary upload failed: {uploadResult.Error.Message}"
                );

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                throw new InvalidArgumentException(
                    paramName: nameof(publicId),
                    message: "PublicId không được null hoặc rỗng."
                );

            try
            {
                var deleteParams = new DeletionParams(publicId) { ResourceType = ResourceType.Image };
                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                if (result.Result != "ok")
                    throw new ErrorException(
                        StatusCodes.Status500InternalServerError,
                        "cloudinary_delete_failed",
                        $"Cloudinary delete failed: {result.Error?.Message ?? "Unknown error"}"
                    );

                return true;
            }
            catch (Exception ex)
            {
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    "cloudinary_error",
                    $"Lỗi xóa ảnh từ Cloudinary: {ex.Message}"
                );
            }
        }
    }
}
