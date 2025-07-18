using System.Text.Json.Serialization;

namespace App.DTOs.PaymentDTOs
{
    #region Request DTOs
    
    public class PayosPaymentRequest
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string? CancelUrl { get; set; }
    }
    
    #endregion
    
    #region Response DTOs
    
    public class PayosPaymentResponse
    {
        public string OrderUrl { get; set; } = string.Empty;
        public string OrderToken { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
    }

    public class PayosOrderStatusResponse
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? PaymentTime { get; set; }
    }
    
    #endregion
    
    #region Internal DTOs for API communication
    
    public class PayosApiResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public PayosApiData Data { get; set; } = new PayosApiData();
    }

    public class PayosApiData
    {
        [JsonPropertyName("orderCode")]
        public string OrderCode { get; set; } = string.Empty;
        
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("qrCode")]
        public string QrCode { get; set; } = string.Empty;
        
        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;
        
        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }
        
        [JsonPropertyName("paymentTime")]
        public DateTime? PaymentTime { get; set; }
    }
    
    #endregion
}