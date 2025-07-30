using App.Repositories.Models.Payment;
using FluentValidation;

namespace App.DTOs.PaymentDTOs
{
    public class SetupFeeRequest
    {
        public string FeeCode { get; set; } = string.Empty;
        public decimal Value { get; set; }
        
        public FeeType Type { get; set; } = FeeType.Percentage;
        public string Description { get; set; } = string.Empty;
    }

    public class SetupFeeRequestValidator : AbstractValidator<SetupFeeRequest>
    {
        public SetupFeeRequestValidator()
        {
            RuleFor(x => x.FeeCode)
                .NotEmpty().WithMessage("Mã phí không được để trống");

            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Giá trị phí không được âm");

            RuleFor(x => x.Value)
                .Must((request, value) => request.Type != FeeType.Percentage || (value >= 0 && value <= 1))
                .WithMessage("Giá trị phí phần trăm phải nằm trong khoảng từ 0 đến 1 (ví dụ: 0.1 tương đương 10%)");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả phí không được để trống");
        }
    }
}