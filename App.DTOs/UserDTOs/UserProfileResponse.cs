using App.Repositories.Models;

namespace App.DTOs.UserDTOs
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPremium { get; set; }
    }

    public static class UserProfileResponseExtensions
    {
        public static UserProfileResponse ToUserProfileResponse(this AppUser user)
        => new UserProfileResponse
        {
            Id = Guid.TryParse(user.Id, out var id) ? id : Guid.Empty,
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
        };
    }
}
