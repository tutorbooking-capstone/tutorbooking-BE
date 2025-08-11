using App.Repositories.Models.Notifications;
using FluentValidation;

namespace App.DTOs.NotificationDTOs
{
    public class NotificationRequest
    {
        public ENotificationPriority NotificationPriority { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? AdditionalData { get; set; }
    }

    public static class NotificationRequestExtensions
    {
        public static NotificationEntity ToEntity(this NotificationRequest request)
        {
            return new NotificationEntity()
            {
                NotificationPriority = request.NotificationPriority,
                Title = request.Title,
                Content = request.Content,
                AdditionalData = request.AdditionalData
            };
        }
    }

    public class NotificationRequestRequestValidator : AbstractValidator<NotificationRequest>
    {
        public NotificationRequestRequestValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(100)
                .WithMessage("TITLE_MAX_100_CHARACTERS");
            RuleFor(x => x.Content)
                .MaximumLength(300)
                .WithMessage("CONTENT_MAX_300_CHARACTERS");
            RuleFor(x => x.AdditionalData)
                .MaximumLength(1000)
                .WithMessage("ADDITIONAL_DATA_MAX_1000_CHARACTERS");
        }
    }

}
