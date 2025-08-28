using App.Repositories.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorIntroductionVideoStatusUpdateRequest
    {
        public string Id { get; set; }
        public TutorIntroductionVideoStatus Status { get; set; }
    }

    public class TutorIntroductionVideoStatusUpdateRequestValidator : AbstractValidator<TutorIntroductionVideoStatusUpdateRequest>
    {
        public TutorIntroductionVideoStatusUpdateRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.");
            RuleFor(x => x.Status)
                .Must(x => x == TutorIntroductionVideoStatus.Active
                || x == TutorIntroductionVideoStatus.Inactive).WithMessage("Invalid status value.");
        }
    }
}

