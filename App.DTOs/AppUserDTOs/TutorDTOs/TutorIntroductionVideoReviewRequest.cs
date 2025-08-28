using App.Repositories.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorIntroductionVideoReviewRequest
    {
        public string Id { get; set; }
        public TutorIntroductionVideoStatus Status{ get; set; }
    }

    public class TutorIntroductionVideoReviewRequestValidator : AbstractValidator<TutorIntroductionVideoReviewRequest>
    {
        public TutorIntroductionVideoReviewRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.");
            RuleFor(x => x.Status)
                .Must(x => x == TutorIntroductionVideoStatus.Active 
                || x == TutorIntroductionVideoStatus.Inactive 
                || x == TutorIntroductionVideoStatus.Rejected).WithMessage("Invalid status value.");
        }
    }
}
