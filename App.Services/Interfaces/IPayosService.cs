using App.DTOs.PaymentDTOs;

namespace App.Services.Interfaces
{
    public interface IPayosService
    {
        // Tạo yêu cầu thanh toán PayOS
        Task<PayosPaymentResponse> CreatePaymentRequestAsync(PayosPaymentRequest request);
        
        // Xác minh callback từ PayOS
        Task<bool> VerifyCallbackAsync(Dictionary<string, string> callbackParams);
        
        // Kiểm tra trạng thái đơn hàng
        Task<PayosOrderStatusResponse> CheckOrderStatusAsync(string orderCode);
    }
}