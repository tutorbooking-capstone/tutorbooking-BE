using App.Repositories.Models;

namespace App.DTOs.PaymentDTOs
{
    public class BankAccountResponse
    {
        public string Id { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
    }

    public class BankAccountRequest
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
    }

    public class WithdrawalRequestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string BankAccountId { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public WithdrawalRequestStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public BankAccountResponse BankAccount { get; set; } = new BankAccountResponse();
    }

    public class WithdrawalRequestRequest
    {
        public string BankAccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}