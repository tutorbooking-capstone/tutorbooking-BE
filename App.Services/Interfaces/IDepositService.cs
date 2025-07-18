using App.Core.Base;
using App.DTOs.PaymentDTOs;

namespace App.Services.Interfaces
{
    public interface IDepositService
    {
        // Tạo yêu cầu nạp tiền
        Task<DepositRequestResponse> CreateDepositRequestAsync(decimal amount);
        
        // Lấy thông tin yêu cầu nạp tiền theo ID
        Task<DepositRequestResponse> GetDepositRequestByIdAsync(string requestId);
        
        // Tạo URL thanh toán PayOS cho yêu cầu nạp tiền
        Task<PayosPaymentResponse> GeneratePayosPaymentAsync(string requestId);
        
        // Xử lý callback từ PayOS
        Task<bool> ProcessPayosCallbackAsync(Dictionary<string, string> payosParams);
        
        // Lấy lịch sử nạp tiền của người dùng hiện tại
        Task<BasePaginatedList<DepositRequestResponse>> GetUserDepositHistoryAsync(int page = 1, int pageSize = 10);
        
        // Lấy tất cả lịch sử nạp tiền (cho admin)
        Task<BasePaginatedList<DepositRequestResponse>> GetAllDepositHistoryAsync(int page = 1, int pageSize = 10);
        
        // Kiểm tra trạng thái nạp tiền
        Task<bool> CheckAndUpdateDepositStatusAsync(string requestId);
    }
}