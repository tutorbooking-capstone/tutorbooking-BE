using App.Repositories.Models;

namespace App.DTOs.PaymentDTOs
{
    public class DepositRequestResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentGateway { get; set; } = string.Empty;
        public DepositRequestStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? PayosOrderUrl { get; set; }
        public string? PayosQrCode { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }

    public class CreateDepositRequest
    {
        public decimal Amount { get; set; }
    }
}
