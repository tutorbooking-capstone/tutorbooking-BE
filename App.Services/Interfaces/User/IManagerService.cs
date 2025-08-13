using App.Core.Base;
using App.DTOs.AppUserDTOs.ManagerDTOs;

namespace App.Services.Interfaces.User
{
    public enum TimeRange
    {
        [EnumDescription("1 ngày gần nhất")]
        Day = 0,
        [EnumDescription("7 ngày gần nhất")]
        Week = 1,
        [EnumDescription("1 tháng gần nhất")]
        Month = 2,
        [EnumDescription("6 tháng gần nhất")]
        HalfYear = 3,
        [EnumDescription("1 năm gần nhất")]
        Year = 4,
        [EnumDescription("Tất cả")]
        All = 5
    }
    

    public interface IManagerService
    {
        // Lấy tổng quan về tiền trong hệ thống
        Task<SystemFinancialOverviewDTO> GetSystemFinancialOverviewAsync();
        
        // Lấy chi tiết số dư từng loại ví
        Task<WalletBalancesDTO> GetWalletBalancesAsync();
        
        // Lấy thông tin tổng hợp về giao dịch
        Task<TransactionSummaryDTO> GetTransactionSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);
        
        // Lấy thông tin số tiền đang giữ (held funds)
        Task<HeldFundsSummaryDTO> GetHeldFundsSummaryAsync();
        
        // Các phương thức metadata hiện có
        Task<Dictionary<string, object>> GetFinancialOverviewMetadataAsync();
        Task<Dictionary<string, object>> GetWalletBalancesMetadataAsync();
        Task<Dictionary<string, object>> GetTransactionSummaryMetadataAsync();
        Task<Dictionary<string, object>> GetHeldFundsMetadataAsync();

        // Các phương thức mới
        // 1. Doanh thu hệ thống theo khoảng thời gian
        Task<SystemRevenueDTO> GetSystemRevenueAsync(TimeRange timeRange = TimeRange.Month);
        Task<Dictionary<string, object>> GetSystemRevenueMetadataAsync();
        
        // 2. Top doanh thu của giáo viên
        Task<List<TutorRevenueDTO>> GetTopTutorRevenueAsync(int top = 10, TimeRange timeRange = TimeRange.Month);
        Task<Dictionary<string, object>> GetTutorRevenueMetadataAsync();
        
        // 3. Thống kê giao dịch theo loại
        Task<TransactionStatisticsDTO> GetTransactionStatisticsAsync(TimeRange timeRange = TimeRange.Month);
        Task<Dictionary<string, object>> GetTransactionStatisticsMetadataAsync();
        
        // 4. Thống kê tiền giữ theo trạng thái
        Task<HeldFundStatisticsDTO> GetHeldFundStatisticsAsync(TimeRange timeRange = TimeRange.Month);
        Task<Dictionary<string, object>> GetHeldFundStatisticsMetadataAsync();
        
        // 5. Lấy danh sách giao dịch gần đây
        Task<BasePaginatedList<RecentTransactionDTO>> GetRecentTransactionsAsync(int page = 1, int pageSize = 10);
        Task<Dictionary<string, object>> GetRecentTransactionsMetadataAsync();
        
        // Helper để chuyển TimeRange thành khoảng thời gian thực tế
        (DateTime? fromDate, DateTime? toDate) GetDateRangeFromTimeRange(TimeRange timeRange);
    
    }
}