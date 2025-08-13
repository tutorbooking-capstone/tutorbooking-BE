using App.Core.Base;
using App.DTOs.AppUserDTOs.ManagerDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Rating;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services.User
{
    public class ManagerService : IManagerService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public ManagerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        #region Tiền và vi và blablabla
        public async Task<SystemFinancialOverviewDTO> GetSystemFinancialOverviewAsync()
        {
            // Lấy dữ liệu từ database
            var deposits = await _unitOfWork.GetRepository<DepositRequest>()
                .ExistEntities()
                .Where(d => d.Status == DepositRequestStatus.Success)
                .ToListAsync();

            var withdrawals = await _unitOfWork.GetRepository<WithdrawalRequest>()
                .ExistEntities()
                .Where(w => w.Status == WithdrawalRequestStatus.Completed)
                .ToListAsync();

            var wallets = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .ToListAsync();

            var heldFunds = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .ToListAsync();

            // Sử dụng mapping từ entity sang DTO
            return SystemFinancialOverviewDTO.FromEntities(deposits, withdrawals, wallets, heldFunds);
        }
        
        public async Task<WalletBalancesDTO> GetWalletBalancesAsync()
        {
            var wallets = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .ToListAsync();

            return WalletBalancesDTO.FromEntities(wallets);
        }
        
        public async Task<TransactionSummaryDTO> GetTransactionSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities()
                .Where(t => t.Status == TransactionStatus.Success);
                
            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
                
            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);
                
            var transactions = await query.ToListAsync();
            return TransactionSummaryDTO.FromEntities(transactions);
        }
        
        public async Task<HeldFundsSummaryDTO> GetHeldFundsSummaryAsync()
        {
            var heldFunds = await _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities()
                .ToListAsync();
                
            return HeldFundsSummaryDTO.FromEntities(heldFunds);
        }
        
        #region Metadata Methods
        public Task<Dictionary<string, object>> GetFinancialOverviewMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Sử dụng EnumHelper để lấy thông tin từ enum attributes
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(DepositRequestStatus),
                typeof(WithdrawalRequestStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Thêm giải thích về các thành phần trong tổng quan tài chính
            var financialComponents = new
            {
                TotalMoneyInCirculation = "Tổng tiền lưu hành = Tổng nạp thành công - Tổng rút thành công",
                TotalSuccessfulDeposits = "Tổng số tiền từ các yêu cầu nạp tiền đã thành công",
                TotalCompletedWithdrawals = "Tổng số tiền từ các yêu cầu rút tiền đã hoàn thành",
                TotalWalletBalances = "Tổng số dư tất cả ví trong hệ thống",
                TotalHeldFunds = "Tổng tiền đang được giữ trong ví escrow"
            };
            metadata.Add("FinancialComponents", financialComponents);
            
            return Task.FromResult(metadata);
        }

        public Task<Dictionary<string, object>> GetWalletBalancesMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Sử dụng EnumHelper để lấy thông tin từ enum attributes
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(WalletType),
                typeof(WalletStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Thêm giải thích về các thành phần trong số dư ví
            var walletBalanceComponents = new
            {
                TotalUserWalletBalances = "Tổng số dư trong các ví cá nhân của người dùng",
                SystemWalletBalance = "Số dư trong ví hệ thống, dùng để lưu trữ phí và doanh thu",
                EscrowWalletBalance = "Số dư trong ví ký quỹ, dùng để giữ tiền tạm thời trước khi thanh toán cho gia sư",
                TotalActiveWallets = "Số lượng ví đang ở trạng thái hoạt động",
                TotalLockedWallets = "Số lượng ví đang bị khóa"
            };
            metadata.Add("WalletBalanceComponents", walletBalanceComponents);
            
            return Task.FromResult(metadata);
        }

        public Task<Dictionary<string, object>> GetTransactionSummaryMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Sử dụng EnumHelper để lấy thông tin từ enum attributes
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(TransactionType),
                typeof(TransactionStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Thêm giải thích về các thành phần trong bảng tóm tắt giao dịch
            var transactionSummaryComponents = new
            {
                TotalDepositAmount = "Tổng số tiền nạp vào hệ thống",
                TotalWithdrawalAmount = "Tổng số tiền rút ra khỏi hệ thống",
                TotalPaymentAmount = "Tổng số tiền thanh toán cho các dịch vụ",
                TotalRefundAmount = "Tổng số tiền hoàn trả cho người dùng",
                TotalCommissionAmount = "Tổng số tiền hoa hồng",
                TotalFeeAmount = "Tổng số tiền phí giao dịch",
                TransactionCountByType = "Số lượng giao dịch theo từng loại",
                TransactionAmountByType = "Tổng số tiền theo từng loại giao dịch"
            };
            metadata.Add("TransactionSummaryComponents", transactionSummaryComponents);
            
            return Task.FromResult(metadata);
        }

        public Task<Dictionary<string, object>> GetHeldFundsMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Sử dụng EnumHelper để lấy thông tin từ enum attributes
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(HeldFundStatus),
                typeof(HeldFundType)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Thêm giải thích về các thành phần trong bảng tóm tắt tiền giữ
            var heldFundComponents = new
            {
                TotalHeldAmount = "Tổng số tiền đang được giữ trong trạng thái Held",
                TotalDisputedAmount = "Tổng số tiền đang trong trạng thái tranh chấp (Disputed)",
                TotalHeldFundsCount = "Tổng số lượng HeldFund records",
                HeldAmountByType = "Tổng số tiền theo từng loại HeldFund",
                HeldCountByType = "Số lượng HeldFund theo từng loại",
                HeldAmountByStatus = "Tổng số tiền theo từng trạng thái HeldFund",
                HeldCountByStatus = "Số lượng HeldFund theo từng trạng thái"
            };
            metadata.Add("HeldFundComponents", heldFundComponents);
            
            return Task.FromResult(metadata);
        }
        #endregion
        #endregion
        
        #region Phương thức hỗ trợ thời gian
        public (DateTime? fromDate, DateTime? toDate) GetDateRangeFromTimeRange(TimeRange timeRange)
        {
            DateTime now = DateTime.UtcNow;
            
            return timeRange switch
            {
                TimeRange.Day => (now.AddDays(-1), now),
                TimeRange.Week => (now.AddDays(-7), now),
                TimeRange.Month => (now.AddMonths(-1), now),
                TimeRange.HalfYear => (now.AddMonths(-6), now),
                TimeRange.Year => (now.AddYears(-1), now),
                TimeRange.All => (null, null),
                _ => (now.AddMonths(-1), now)
            };
        }
        #endregion
        
        #region Doanh thu hệ thống
        public async Task<SystemRevenueDTO> GetSystemRevenueAsync(TimeRange timeRange = TimeRange.Month)
        {
            var (fromDate, toDate) = GetDateRangeFromTimeRange(timeRange);
            
            // Lấy ví hệ thống
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);
                
            if (systemWallet == null)
                throw new Exception("Không tìm thấy ví hệ thống");
            
            // Query các giao dịch trong khoảng thời gian
            var transactionsQuery = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities()
                .Where(t => t.Status == TransactionStatus.Success);
                
            if (fromDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt >= fromDate.Value);
                
            if (toDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt <= toDate.Value);
            
            var transactions = await transactionsQuery
                .Where(t => (t.Type == TransactionType.Commission || t.Type == TransactionType.Fee)
                            && t.TargetWalletId == systemWallet.Id)
                .ToListAsync();
                
            // Sử dụng phương thức mapping
            return SystemRevenueDTO.FromEntities(transactions, systemWallet);
        }
        
        public Task<Dictionary<string, object>> GetSystemRevenueMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Thông tin về TimeRange
            var timeRangeValues = EnumHelper.GetEnumMetadata(typeof(TimeRange));
            metadata.Add("TimeRange", timeRangeValues["TimeRange"]);
            
            // Giải thích các thành phần trong SystemRevenueDTO
            var components = new
            {
                TotalCommission = "Tổng hoa hồng thu được từ các giao dịch thanh toán",
                TotalFees = "Tổng phí dịch vụ thu được từ các giao dịch",
                TotalRevenue = "Tổng doanh thu (Commission + Fees)",
                SystemWalletBalance = "Số dư hiện tại của ví hệ thống",
                RevenueByDay = "Doanh thu theo từng ngày (định dạng YYYY-MM-DD)",
                RevenueByType = "Doanh thu theo từng loại giao dịch"
            };
            metadata.Add("SystemRevenueComponents", components);
            
            return Task.FromResult(metadata);
        }
        #endregion
        
        #region Top doanh thu giáo viên
        public async Task<List<TutorRevenueDTO>> GetTopTutorRevenueAsync(int top = 10, TimeRange timeRange = TimeRange.Month)
        {
            var (fromDate, toDate) = GetDateRangeFromTimeRange(timeRange);
            
            // Query tất cả giao dịch thanh toán thành công cho giáo viên
            var transactionsQuery = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities()
                .Where(t => t.Status == TransactionStatus.Success 
                            && t.Type == TransactionType.Payment);
                
            if (fromDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt >= fromDate.Value);
                
            if (toDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt <= toDate.Value);
            
            // Lấy tất cả giáo viên và wallet của họ
            var tutors = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .Include(t => t.User)
                .ToListAsync();
                
            var tutorUserIds = tutors.Select(t => t.UserId).ToList();
            
            var wallets = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .Where(w => tutorUserIds.Contains(w.UserId!))
                .ToListAsync();
                
            // Lấy các giao dịch thanh toán cho các ví của giáo viên
            var transactions = await transactionsQuery
                .Include(t => t.TargetWallet)
                .Where(t => t.TargetWallet != null && tutorUserIds.Contains(t.TargetWallet.UserId!))
                .ToListAsync();
            
            // Sử dụng phương thức mapping
            return TutorRevenueDTO.FromEntities(tutors, wallets, transactions, top);
        }
        
        public Task<Dictionary<string, object>> GetTutorRevenueMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Thông tin về TimeRange
            var timeRangeValues = EnumHelper.GetEnumMetadata(typeof(TimeRange));
            metadata.Add("TimeRange", timeRangeValues["TimeRange"]);
            
            // Giải thích các thành phần trong TutorRevenueDTO
            var components = new
            {
                TutorId = "ID của giáo viên",
                TutorName = "Tên giáo viên",
                Email = "Email của giáo viên",
                TotalRevenue = "Tổng doanh thu từ việc dạy học",
                CompletedLessons = "Số buổi học đã hoàn thành",
                AverageRating = "Đánh giá trung bình từ học viên",
                WalletBalance = "Số dư ví hiện tại của giáo viên"
            };
            metadata.Add("TutorRevenueComponents", components);
            
            return Task.FromResult(metadata);
        }
        #endregion
        
        #region Thống kê giao dịch
        public async Task<TransactionStatisticsDTO> GetTransactionStatisticsAsync(TimeRange timeRange = TimeRange.Month)
        {
            var (fromDate, toDate) = GetDateRangeFromTimeRange(timeRange);
            
            // Query các giao dịch trong khoảng thời gian
            var transactionsQuery = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities();
                
            if (fromDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt >= fromDate.Value);
                
            if (toDate.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt <= toDate.Value);
            
            var transactions = await transactionsQuery.ToListAsync();
            
            // Sử dụng phương thức mapping
            return TransactionStatisticsDTO.FromEntities(transactions);
        }
        
        public Task<Dictionary<string, object>> GetTransactionStatisticsMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Thông tin về TimeRange, TransactionType và TransactionStatus
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(TimeRange),
                typeof(TransactionType),
                typeof(TransactionStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Giải thích các thành phần trong TransactionStatisticsDTO
            var components = new
            {
                TotalTransactionCount = "Tổng số giao dịch",
                TotalTransactionAmount = "Tổng giá trị giao dịch",
                CountByType = "Số lượng giao dịch theo từng loại",
                AmountByType = "Tổng giá trị giao dịch theo từng loại",
                CountByStatus = "Số lượng giao dịch theo từng trạng thái",
                AmountByStatus = "Tổng giá trị giao dịch theo từng trạng thái",
                CountByDay = "Số lượng giao dịch theo từng ngày (định dạng YYYY-MM-DD)",
                AmountByDay = "Tổng giá trị giao dịch theo từng ngày (định dạng YYYY-MM-DD)"
            };
            metadata.Add("TransactionStatisticsComponents", components);
            
            return Task.FromResult(metadata);
        }
        #endregion
        
        #region Thống kê tiền giữ
        public async Task<HeldFundStatisticsDTO> GetHeldFundStatisticsAsync(TimeRange timeRange = TimeRange.Month)
        {
            var (fromDate, toDate) = GetDateRangeFromTimeRange(timeRange);
            
            // Query các tiền giữ trong khoảng thời gian
            var heldFundsQuery = _unitOfWork.GetRepository<HeldFund>()
                .ExistEntities();
                
            if (fromDate.HasValue)
                heldFundsQuery = heldFundsQuery.Where(h => h.CreatedAt >= fromDate.Value);
                
            if (toDate.HasValue)
                heldFundsQuery = heldFundsQuery.Where(h => h.CreatedAt <= toDate.Value);
            
            var heldFunds = await heldFundsQuery.ToListAsync();
            
            // Sử dụng phương thức mapping
            return HeldFundStatisticsDTO.FromEntities(heldFunds);
        }
        
        public Task<Dictionary<string, object>> GetHeldFundStatisticsMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Thông tin về TimeRange, HeldFundStatus và HeldFundType
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(TimeRange),
                typeof(HeldFundStatus),
                typeof(HeldFundType)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Giải thích các thành phần trong HeldFundStatisticsDTO
            var components = new
            {
                TotalHeldFundsCount = "Tổng số tiền giữ",
                TotalHeldAmount = "Tổng giá trị tiền giữ",
                CountByStatus = "Số lượng tiền giữ theo từng trạng thái",
                AmountByStatus = "Tổng giá trị tiền giữ theo từng trạng thái",
                CountByType = "Số lượng tiền giữ theo từng loại",
                AmountByType = "Tổng giá trị tiền giữ theo từng loại"
            };
            metadata.Add("HeldFundStatisticsComponents", components);
            
            return Task.FromResult(metadata);
        }
        #endregion
        
        #region Giao dịch gần đây
        public async Task<BasePaginatedList<RecentTransactionDTO>> GetRecentTransactionsAsync(int page = 1, int pageSize = 10)
        {
            // Query các giao dịch gần đây nhất
            var transactionsQuery = _unitOfWork.GetRepository<Transaction>()
                .ExistEntities()
                .OrderByDescending(t => t.CreatedAt)
                .Include(t => t.SourceWallet)
                .ThenInclude(w => w!.User)
                .Include(t => t.TargetWallet)
                .ThenInclude(w => w!.User);
            
            var totalCount = await transactionsQuery.CountAsync();
            
            var transactions = await transactionsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var recentTransactions = RecentTransactionDTO.FromEntities(transactions, GetUserName);
            
            return new BasePaginatedList<RecentTransactionDTO>(recentTransactions, totalCount, page, pageSize);
        }
        
        private string? GetUserName(Wallet? wallet)
        {
            if (wallet == null) return null;
            
            if (wallet.Type == WalletType.System) return "Hệ thống";
            if (wallet.Type == WalletType.Escrow) return "Ví ký quỹ";
            
            return wallet.User?.FullName ?? wallet.UserId;
        }
        
        public Task<Dictionary<string, object>> GetRecentTransactionsMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            // Thông tin về TransactionType và TransactionStatus
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(TransactionType),
                typeof(TransactionStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            // Giải thích về phân trang
            var paginationInfo = new
            {
                Page = "Số trang hiện tại (bắt đầu từ 1)",
                PageSize = "Số lượng bản ghi trên mỗi trang",
                TotalCount = "Tổng số bản ghi",
                TotalPages = "Tổng số trang"
            };
            metadata.Add("PaginationInfo", paginationInfo);
            
            // Giải thích các thành phần trong RecentTransactionDTO
            var components = new
            {
                Id = "ID của giao dịch",
                SourceUser = "Người dùng/ví nguồn (người gửi)",
                TargetUser = "Người dùng/ví đích (người nhận)",
                Amount = "Số tiền giao dịch",
                Type = "Loại giao dịch",
                Status = "Trạng thái giao dịch",
                Description = "Mô tả giao dịch",
                CreatedAt = "Thời gian tạo giao dịch"
            };
            metadata.Add("RecentTransactionComponents", components);
            
            return Task.FromResult(metadata);
        }
        #endregion
    }
}