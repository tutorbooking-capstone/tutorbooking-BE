using App.Core.Base;
using App.DTOs.AppUserDTOs.ManagerDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(Role.Manager)]  
    public class ManagerController : ControllerBase
    {
        private readonly IManagerService _managerService;

        public ManagerController(IManagerService managerService)
        {
            _managerService = managerService;
        }

        [HttpGet("financial-overview")]
        public async Task<IActionResult> GetFinancialOverview()
        {
            var overview = await _managerService.GetSystemFinancialOverviewAsync();
            var metadata = await _managerService.GetFinancialOverviewMetadataAsync();
            
            return Ok(new BaseResponseModel<SystemFinancialOverviewDTO>(
                data: overview,
                additionalData: metadata,
                message: "Tổng quan tài chính hệ thống"
            ));
        }

        [HttpGet("wallet-balances")]
        public async Task<IActionResult> GetWalletBalances()
        {
            var balances = await _managerService.GetWalletBalancesAsync();
            var metadata = await _managerService.GetWalletBalancesMetadataAsync();
            
            return Ok(new BaseResponseModel<WalletBalancesDTO>(
                data: balances,
                additionalData: metadata,
                message: "Thông tin số dư từng loại ví"
            ));
        }

        [HttpGet("transaction-summary")]
        public async Task<IActionResult> GetTransactionSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var summary = await _managerService.GetTransactionSummaryAsync(fromDate, toDate);
            var metadata = await _managerService.GetTransactionSummaryMetadataAsync();
            
            return Ok(new BaseResponseModel<TransactionSummaryDTO>(
                data: summary,
                additionalData: metadata,
                message: "Tổng hợp giao dịch"
            ));
        }

        [HttpGet("held-funds-summary")]
        public async Task<IActionResult> GetHeldFundsSummary()
        {
            var summary = await _managerService.GetHeldFundsSummaryAsync();
            var metadata = await _managerService.GetHeldFundsMetadataAsync();
            
            return Ok(new BaseResponseModel<HeldFundsSummaryDTO>(
                data: summary,
                additionalData: metadata,
                message: "Tổng hợp tiền giữ đang giữ trong ví escrow"
            ));
        }
        
        [HttpGet("financial-overview/metadata")]
        public async Task<IActionResult> GetFinancialOverviewMetadata()
        {
            var metadata = await _managerService.GetFinancialOverviewMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho tổng quan tài chính"
            ));
        }
        
        [HttpGet("wallet-balances/metadata")]
        public async Task<IActionResult> GetWalletBalancesMetadata()
        {
            var metadata = await _managerService.GetWalletBalancesMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho thông tin số dư ví"
            ));
        }
        
        [HttpGet("transaction-summary/metadata")]
        public async Task<IActionResult> GetTransactionSummaryMetadata()
        {
            var metadata = await _managerService.GetTransactionSummaryMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho tổng hợp giao dịch"
            ));
        }
        
        [HttpGet("held-funds-summary/metadata")]
        public async Task<IActionResult> GetHeldFundsSummaryMetadata()
        {
            var metadata = await _managerService.GetHeldFundsMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho tổng hợp tiền giữ"
            ));
        }

        [HttpGet("system-revenue")]
        public async Task<IActionResult> GetSystemRevenue([FromQuery] TimeRange timeRange = TimeRange.Month)
        {
            var revenue = await _managerService.GetSystemRevenueAsync(timeRange);
            var metadata = await _managerService.GetSystemRevenueMetadataAsync();
            
            return Ok(new BaseResponseModel<SystemRevenueDTO>(
                data: revenue,
                additionalData: metadata,
                message: "Doanh thu hệ thống"
            ));
        }

        [HttpGet("top-tutors")]
        public async Task<IActionResult> GetTopTutorRevenue(
            [FromQuery] int top = 10,
            [FromQuery] TimeRange timeRange = TimeRange.Month)
        {
            var tutors = await _managerService.GetTopTutorRevenueAsync(top, timeRange);
            var metadata = await _managerService.GetTutorRevenueMetadataAsync();
            
            return Ok(new BaseResponseModel<List<TutorRevenueDTO>>(
                data: tutors,
                additionalData: metadata,
                message: $"Top {top} giáo viên có doanh thu cao nhất"
            ));
        }

        [HttpGet("transaction-statistics")]
        public async Task<IActionResult> GetTransactionStatistics([FromQuery] TimeRange timeRange = TimeRange.Month)
        {
            var statistics = await _managerService.GetTransactionStatisticsAsync(timeRange);
            var metadata = await _managerService.GetTransactionStatisticsMetadataAsync();
            
            return Ok(new BaseResponseModel<TransactionStatisticsDTO>(
                data: statistics,
                additionalData: metadata,
                message: "Thống kê giao dịch"
            ));
        }

        [HttpGet("held-fund-statistics")]
        public async Task<IActionResult> GetHeldFundStatistics([FromQuery] TimeRange timeRange = TimeRange.Month)
        {
            var statistics = await _managerService.GetHeldFundStatisticsAsync(timeRange);
            var metadata = await _managerService.GetHeldFundStatisticsMetadataAsync();
            
            return Ok(new BaseResponseModel<HeldFundStatisticsDTO>(
                data: statistics,
                additionalData: metadata,
                message: "Thống kê tiền giữ"
            ));
        }

        [HttpGet("recent-transactions")]
        public async Task<IActionResult> GetRecentTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var transactions = await _managerService.GetRecentTransactionsAsync(page, pageSize);
            var metadata = await _managerService.GetRecentTransactionsMetadataAsync();
            
            return Ok(new BaseResponseModel<BasePaginatedList<RecentTransactionDTO>>(
                data: transactions,
                additionalData: metadata,
                message: "Các giao dịch gần đây"
            ));
        }

        [HttpGet("system-revenue/metadata")]
        public async Task<IActionResult> GetSystemRevenueMetadata()
        {
            var metadata = await _managerService.GetSystemRevenueMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho doanh thu hệ thống"
            ));
        }

        [HttpGet("top-tutors/metadata")]
        public async Task<IActionResult> GetTopTutorRevenueMetadata()
        {
            var metadata = await _managerService.GetTutorRevenueMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho top doanh thu giáo viên"
            ));
        }

        [HttpGet("transaction-statistics/metadata")]
        public async Task<IActionResult> GetTransactionStatisticsMetadata()
        {
            var metadata = await _managerService.GetTransactionStatisticsMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho thống kê giao dịch"
            ));
        }

        [HttpGet("held-fund-statistics/metadata")]
        public async Task<IActionResult> GetHeldFundStatisticsMetadata()
        {
            var metadata = await _managerService.GetHeldFundStatisticsMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho thống kê tiền giữ"
            ));
        }

        [HttpGet("recent-transactions/metadata")]
        public async Task<IActionResult> GetRecentTransactionsMetadata()
        {
            var metadata = await _managerService.GetRecentTransactionsMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho giao dịch gần đây"
            ));
        }
    }
}