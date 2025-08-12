using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorIntroductionVideoRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public static class TutorIntroductionVideoRequestExtensions
    {
        public static TutorIntroductionVideo ToEntity(this TutorIntroductionVideoRequest request)
        {
            return new TutorIntroductionVideo
            {
                Url = request.Url
            };
        }
    }

    public class TutorIntroductionVideoRequestValidator : AbstractValidator<TutorIntroductionVideoRequest>
    {
        public TutorIntroductionVideoRequestValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Video URL is required.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Invalid video URL format.");
        }
    }
}
