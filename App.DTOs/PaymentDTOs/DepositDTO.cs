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

        public static DepositRequestResponse FromEntity(DepositRequest entity)
        {
            return new DepositRequestResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Amount = entity.Amount,
                PaymentGateway = entity.PaymentGateway,
                Status = entity.Status,
                CreatedTime = entity.CreatedTime.DateTime,
                CompletedAt = entity.CompletedAt,
                PayosOrderUrl = entity.PayosOrderUrl,
                PayosQrCode = entity.PayosQrCode,
                UserFullName = entity.User?.FullName ?? string.Empty,
            };
        }
    }

    public class CreateDepositRequest
    {
        public decimal Amount { get; set; }
    }

    public class CreateFakeDepositRequest
    {
        public decimal Amount { get; set; }
    }
}
