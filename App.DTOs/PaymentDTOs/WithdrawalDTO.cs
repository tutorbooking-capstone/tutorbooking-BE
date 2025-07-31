using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.PaymentDTOs
{
    public class BankAccountResponse
    {
        public string Id { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;

        public static BankAccountResponse FromEntity(BankAccount entity)
        {
            return new BankAccountResponse
            {
                Id = entity.Id,
                BankName = entity.BankName,
                AccountNumber = entity.AccountNumber,
                AccountHolderName = entity.AccountHolderName
            };
        }
    }

    public class BankAccountRequest
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;

        public BankAccount ToEntity(string userId)
        {
            return new BankAccount
            {
                UserId = userId,
                BankName = BankName,
                AccountNumber = AccountNumber,
                AccountHolderName = AccountHolderName
            };
        }
    }

    public class BankAccountRequestValidator : AbstractValidator<BankAccountRequest>
    {
        public BankAccountRequestValidator()
        {
            RuleFor(x => x.BankName)
                .NotEmpty().WithMessage("Tên ngân hàng không được để trống");

            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Số tài khoản không được để trống")
                .Matches("^[0-9]{9,14}$").WithMessage("Số tài khoản phải gồm 9–14 chữ số số");

            RuleFor(x => x.AccountHolderName)
                .NotEmpty().WithMessage("Tên chủ tài khoản không được để trống")
                .MaximumLength(100);
        }
    }

    public class CreateWithdrawalRequest
    {
        public string BankAccountId { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
    }

    public class CreateWithdrawalRequestValidator : AbstractValidator<CreateWithdrawalRequest>
    {
        public CreateWithdrawalRequestValidator()
        {
            RuleFor(x => x.BankAccountId)
                .NotEmpty().WithMessage("ID tài khoản ngân hàng không được để trống");

            RuleFor(x => x.GrossAmount)
                .GreaterThan(0).WithMessage("Số tiền rút phải lớn hơn 0");
        }
    }

    public class WithdrawalRequestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string BankAccountId { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public WithdrawalRequestStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public BankAccountResponse BankAccount { get; set; } = new BankAccountResponse();

        public static WithdrawalRequestResponse FromEntity(WithdrawalRequest entity)
        {
            return new WithdrawalRequestResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                UserFullName = entity.User?.FullName ?? string.Empty,
                BankAccountId = entity.BankAccountId,
                GrossAmount = entity.GrossAmount,
                NetAmount = entity.NetAmount,
                Status = entity.Status,
                CreatedTime = entity.CreatedTime.DateTime,
                CompletedAt = entity.CompletedAt,
                RejectionReason = entity.RejectionReason,
                BankAccount = entity.BankAccount != null ? new BankAccountResponse
                {
                    Id = entity.BankAccount.Id,
                    BankName = entity.BankAccount.BankName,
                    AccountNumber = entity.BankAccount.AccountNumber,
                    AccountHolderName = entity.BankAccount.AccountHolderName
                } : new BankAccountResponse()
            };
        }
    }

    public class ProcessWithdrawalRequest
    {
        public string WithdrawalId { get; set; } = string.Empty;
    }

    public class RejectWithdrawalRequest
    {
        public string WithdrawalId { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class RejectWithdrawalRequestValidator : AbstractValidator<RejectWithdrawalRequest>
    {
        public RejectWithdrawalRequestValidator()
        {
            RuleFor(x => x.WithdrawalId)
                .NotEmpty().WithMessage("ID yêu cầu rút tiền không được để trống");

            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("Lý do từ chối không được để trống")
                .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự");
        }
    }
}