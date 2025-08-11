using App.Repositories.Models.User;

namespace App.DTOs.NotificationDTOs
{
    public class NotificationSenderResponse
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }

    public static class NotificationSenderResponseExtensions
    {
        public static NotificationSenderResponse ToNotificationSenderResponse(this AppUser user)
        {
            return new NotificationSenderResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public static NotificationSenderResponse ToNotificationSenderResponse(this Tutor tutor)
        {
            return new NotificationSenderResponse
            {
                Id = tutor.UserId,
                FullName = tutor.NickName,
                ProfilePictureUrl = tutor.User.ProfilePictureUrl
            };
        }
    }
}
