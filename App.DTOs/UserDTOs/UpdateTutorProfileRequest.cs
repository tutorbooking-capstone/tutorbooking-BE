using App.Repositories.Models.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.UserDTOs
{
    public class UpdateTutorProfileRequest
    {
        public string NickName { get; set; }
        public string Brief { get; set; } 
        public string Description { get; set; } 
        public string TeachingMethod { get; set; }
    }

    public class UpdateTutorProfileRequestValidator : AbstractValidator<UpdateTutorProfileRequest>
    {
        public UpdateTutorProfileRequestValidator()
        {
            RuleFor(x => x.NickName)
                .NotEmpty().WithMessage("NickName is required.")
                .MaximumLength(50).WithMessage("NickName cannot exceed 50 characters.");
            RuleFor(x => x.Brief)
                .NotEmpty().WithMessage("Brief is required.")
                .MaximumLength(200).WithMessage("Brief cannot exceed 200 characters.");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
            RuleFor(x => x.TeachingMethod)
                .NotEmpty().WithMessage("TeachingMethod is required.")
                .MaximumLength(500).WithMessage("TeachingMethod cannot exceed 500 characters.");
        }
    }


    public static class UpdateTutorProfileRequestExtensions
    {
        public static void Update(this Tutor tutor, UpdateTutorProfileRequest request)
        {
            tutor.NickName = request.NickName;
            tutor.Brief = request.Brief;
            tutor.Description = request.Description;
            tutor.TeachingMethod = request.TeachingMethod;
        }
    } 
}
