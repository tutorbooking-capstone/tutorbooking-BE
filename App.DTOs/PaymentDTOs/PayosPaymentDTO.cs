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
        public string? OrderUrl { get; set; }
        public string? OrderToken { get; set; }
        public string? QrCode { get; set; }
        public long NumericOrderCode { get; set; }
    }

    public class PayosOrderStatusResponse
    {
        public long OrderCode { get; set; }
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
        public long OrderCode { get; set; }
        
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

    public class PayosCallbackData
    {
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }
        
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
        
        [JsonPropertyName("transactionDateTime")]
        public string? TransactionDateTime { get; set; }
        
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        
        [JsonPropertyName("desc")]
        public string? Desc { get; set; }
    }
}