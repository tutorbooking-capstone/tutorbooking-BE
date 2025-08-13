using App.Repositories.Models;
using App.Repositories.Models.User;

namespace App.DTOs.AppUserDTOs.ManagerDTOs
{

    public class SystemRevenueDTO
    {
        public decimal TotalCommission { get; set; } // Tổng hoa hồng thu được
        public decimal TotalFees { get; set; } // Tổng phí dịch vụ thu được
        public decimal TotalRevenue { get; set; } // Tổng doanh thu (Commission + Fees)
        public decimal SystemWalletBalance { get; set; } // Số dư ví hệ thống
        public Dictionary<string, decimal> RevenueByDay { get; set; } = new(); // Doanh thu theo ngày
        public Dictionary<string, decimal> RevenueByType { get; set; } = new(); // Doanh thu theo loại

        public static SystemRevenueDTO FromEntities(List<Transaction> transactions, Wallet systemWallet)
        {
            var revenueTransactions = transactions
                .Where(t => (t.Type == TransactionType.Commission || t.Type == TransactionType.Fee)
                            && t.TargetWalletId == systemWallet.Id)
                .ToList();
            
            // Tính tổng doanh thu
            decimal totalCommission = revenueTransactions
                .Where(t => t.Type == TransactionType.Commission)
                .Sum(t => t.Amount);
                
            decimal totalFees = revenueTransactions
                .Where(t => t.Type == TransactionType.Fee)
                .Sum(t => t.Amount);
                
            // Tính doanh thu theo ngày
            var revenueByDay = revenueTransactions
                .GroupBy(t => t.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Sum(t => t.Amount)
                );
                
            // Tính doanh thu theo loại
            var revenueByType = revenueTransactions
                .GroupBy(t => t.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Sum(t => t.Amount)
                );
                
            return new SystemRevenueDTO
            {
                TotalCommission = totalCommission,
                TotalFees = totalFees,
                TotalRevenue = totalCommission + totalFees,
                SystemWalletBalance = systemWallet.Balance,
                RevenueByDay = revenueByDay,
                RevenueByType = revenueByType
            };
        }
    }
    
    public class TutorRevenueDTO
    {
        public string TutorId { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; } // Tổng doanh thu từ việc dạy học
        public int CompletedLessons { get; set; } // Số buổi học đã hoàn thành
        public decimal WalletBalance { get; set; } // Số dư ví hiện tại

        public static TutorRevenueDTO FromEntity(Tutor tutor, Wallet? tutorWallet, List<Transaction> tutorTransactions)
        {
            return new TutorRevenueDTO
            {
                TutorId = tutor.UserId,
                TutorName = tutor.User?.FullName ?? "Chưa có tên",
                Email = tutor.User?.Email ?? "Không có email",
                TotalRevenue = tutorTransactions.Sum(t => t.Amount),
                CompletedLessons = tutorTransactions.Count,
                WalletBalance = tutorWallet?.Balance ?? 0
            };
        }

        public static List<TutorRevenueDTO> FromEntities(
            List<Tutor> tutors, 
            List<Wallet> wallets, 
            List<Transaction> transactions,
            int top)
        {
            return tutors
                .Select(tutor => 
                {
                    var tutorWallet = wallets.FirstOrDefault(w => w.UserId == tutor.UserId);
                    var tutorTransactions = transactions
                        .Where(t => t.TargetWallet?.UserId == tutor.UserId)
                        .ToList();
                    
                    return FromEntity(tutor, tutorWallet, tutorTransactions);
                })
                .OrderByDescending(t => t.TotalRevenue)
                .Take(top)
                .ToList();
        }
    }
    
    public class TransactionStatisticsDTO
    {
        public int TotalTransactionCount { get; set; } // Tổng số giao dịch
        public decimal TotalTransactionAmount { get; set; } // Tổng giá trị giao dịch
        public Dictionary<string, int> CountByType { get; set; } = new(); // Số lượng theo loại
        public Dictionary<string, decimal> AmountByType { get; set; } = new(); // Giá trị theo loại
        public Dictionary<string, int> CountByStatus { get; set; } = new(); // Số lượng theo trạng thái
        public Dictionary<string, decimal> AmountByStatus { get; set; } = new(); // Giá trị theo trạng thái
        public Dictionary<string, int> CountByDay { get; set; } = new(); // Số lượng theo ngày
        public Dictionary<string, decimal> AmountByDay { get; set; } = new(); // Giá trị theo ngày

        public static TransactionStatisticsDTO FromEntities(List<Transaction> transactions)
        {
            // Tính toán thống kê cơ bản
            var totalCount = transactions.Count;
            var totalAmount = transactions.Sum(t => t.Amount);
            
            // Thống kê theo loại
            var countByType = transactions
                .GroupBy(t => t.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count()
                );
                
            var amountByType = transactions
                .GroupBy(t => t.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Sum(t => t.Amount)
                );
            
            // Thống kê theo trạng thái
            var countByStatus = transactions
                .GroupBy(t => t.Status)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count()
                );
                
            var amountByStatus = transactions
                .GroupBy(t => t.Status)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Sum(t => t.Amount)
                );
                
            // Thống kê theo ngày
            var countByDay = transactions
                .GroupBy(t => t.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Count()
                );
                
            var amountByDay = transactions
                .GroupBy(t => t.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Sum(t => t.Amount)
                );
                
            return new TransactionStatisticsDTO
            {
                TotalTransactionCount = totalCount,
                TotalTransactionAmount = totalAmount,
                CountByType = countByType,
                AmountByType = amountByType,
                CountByStatus = countByStatus,
                AmountByStatus = amountByStatus,
                CountByDay = countByDay,
                AmountByDay = amountByDay
            };
        }
    }
    
    public class HeldFundStatisticsDTO
    {
        public int TotalHeldFundsCount { get; set; } // Tổng số tiền giữ
        public decimal TotalHeldAmount { get; set; } // Tổng giá trị tiền giữ
        public Dictionary<string, int> CountByStatus { get; set; } = new(); // Số lượng theo trạng thái
        public Dictionary<string, decimal> AmountByStatus { get; set; } = new(); // Giá trị theo trạng thái
        public Dictionary<string, int> CountByType { get; set; } = new(); // Số lượng theo loại
        public Dictionary<string, decimal> AmountByType { get; set; } = new(); // Giá trị theo loại

        public static HeldFundStatisticsDTO FromEntities(List<HeldFund> heldFunds)
        {
            // Tính toán thống kê
            var totalCount = heldFunds.Count;
            var totalAmount = heldFunds.Sum(h => h.Amount);
            
            // Thống kê theo trạng thái
            var countByStatus = heldFunds
                .GroupBy(h => h.Status)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count()
                );
                
            var amountByStatus = heldFunds
                .GroupBy(h => h.Status)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Sum(h => h.Amount)
                );
                
            // Thống kê theo loại
            var countByType = heldFunds
                .GroupBy(h => h.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count()
                );
                
            var amountByType = heldFunds
                .GroupBy(h => h.Type)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Sum(h => h.Amount)
                );
                
            return new HeldFundStatisticsDTO
            {
                TotalHeldFundsCount = totalCount,
                TotalHeldAmount = totalAmount,
                CountByStatus = countByStatus,
                AmountByStatus = amountByStatus,
                CountByType = countByType,
                AmountByType = amountByType
            };
        }
    }
    
    public class RecentTransactionDTO
    {
        public string Id { get; set; } = string.Empty;
        public string? SourceUser { get; set; }
        public string? TargetUser { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public static RecentTransactionDTO FromEntity(Transaction transaction, string? sourceUserName, string? targetUserName)
        {
            return new RecentTransactionDTO
            {
                Id = transaction.Id,
                SourceUser = sourceUserName,
                TargetUser = targetUserName,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };
        }

        public static List<RecentTransactionDTO> FromEntities(List<Transaction> transactions, Func<Wallet?, string?> getUserNameFunc)
        {
            return transactions.Select(t => FromEntity(
                t, 
                getUserNameFunc(t.SourceWallet), 
                getUserNameFunc(t.TargetWallet))
            ).ToList();
        }
    }
}