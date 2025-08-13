using App.Repositories.Models;

namespace App.DTOs.AppUserDTOs.ManagerDTOs
{

    public class SystemFinancialOverviewDTO
    {
        public decimal TotalMoneyInCirculation { get; set; } // Tổng tiền lưu hành
        public decimal TotalSuccessfulDeposits { get; set; } // Tổng nạp thành công
        public decimal TotalCompletedWithdrawals { get; set; } // Tổng rút thành công
        public decimal TotalWalletBalances { get; set; } // Tổng số dư các ví
        public decimal TotalHeldFunds { get; set; } // Tổng tiền đang giữ

        public static SystemFinancialOverviewDTO FromEntities(
            List<DepositRequest> deposits,
            List<WithdrawalRequest> withdrawals,
            List<Wallet> wallets,
            List<HeldFund> heldFunds)
        {
            return new SystemFinancialOverviewDTO
            {
                TotalSuccessfulDeposits = deposits
                    .Where(d => d.Status == DepositRequestStatus.Success)
                    .Sum(d => d.Amount),
                TotalCompletedWithdrawals = withdrawals
                    .Where(w => w.Status == WithdrawalRequestStatus.Completed)
                    .Sum(w => w.NetAmount),
                TotalWalletBalances = wallets.Sum(w => w.Balance),
                TotalHeldFunds = heldFunds
                    .Where(h => h.Status == HeldFundStatus.Held || h.Status == HeldFundStatus.Disputed)
                    .Sum(h => h.Amount),
                TotalMoneyInCirculation = deposits
                    .Where(d => d.Status == DepositRequestStatus.Success)
                    .Sum(d => d.Amount) - withdrawals
                    .Where(w => w.Status == WithdrawalRequestStatus.Completed)
                    .Sum(w => w.NetAmount)
            };
        }
    }
    
    public class WalletBalancesDTO 
    {
        public decimal TotalUserWalletBalances { get; set; } // Tổng số dư ví người dùng
        public decimal SystemWalletBalance { get; set; } // Số dư ví hệ thống
        public decimal EscrowWalletBalance { get; set; } // Số dư ví escrow
        public int TotalActiveWallets { get; set; } // Tổng số ví đang hoạt động
        public int TotalLockedWallets { get; set; } // Tổng số ví bị khóa

        public static WalletBalancesDTO FromEntities(List<Wallet> wallets)
        {
            var walletStats = wallets
                .GroupBy(w => w.Type)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalBalance = g.Sum(w => w.Balance),
                    Count = g.Count()
                });

            return new WalletBalancesDTO
            {
                TotalUserWalletBalances = walletStats
                    .FirstOrDefault(w => w.Key == WalletType.Personal).Value?.TotalBalance ?? 0,
                SystemWalletBalance = walletStats
                    .FirstOrDefault(w => w.Key == WalletType.System).Value?.TotalBalance ?? 0,
                EscrowWalletBalance = walletStats
                    .FirstOrDefault(w => w.Key == WalletType.Escrow).Value?.TotalBalance ?? 0,
                TotalActiveWallets = wallets.Count(w => w.Status == WalletStatus.Active),
                TotalLockedWallets = wallets.Count(w => w.Status == WalletStatus.Locked)
            };
        }
    }
    
    public class TransactionSummaryDTO
    {
        public decimal TotalDepositAmount { get; set; } // Tổng tiền nạp
        public decimal TotalWithdrawalAmount { get; set; } // Tổng tiền rút
        public decimal TotalPaymentAmount { get; set; } // Tổng thanh toán
        public decimal TotalRefundAmount { get; set; } // Tổng hoàn tiền
        public decimal TotalCommissionAmount { get; set; } // Tổng hoa hồng
        public decimal TotalFeeAmount { get; set; } // Tổng phí
        public int TotalTransactionCount { get; set; } // Tổng số giao dịch
        public Dictionary<string, int> TransactionCountByType { get; set; } = new(); // Số giao dịch theo loại
        public Dictionary<string, decimal> TransactionAmountByType { get; set; } = new(); // Số tiền theo loại

        public static TransactionSummaryDTO FromEntities(List<Transaction> transactions)
        {
            var summary = new TransactionSummaryDTO();
            var transactionStats = transactions
                .GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalAmount = g.Sum(t => t.Amount),
                    Count = g.Count()
                });

            summary.TotalTransactionCount = transactions.Count;

            foreach (var stat in transactionStats)
            {
                var typeName = stat.Key.ToString();
                summary.TransactionCountByType[typeName] = stat.Value.Count;
                summary.TransactionAmountByType[typeName] = stat.Value.TotalAmount;

                switch (stat.Key)
                {
                    case TransactionType.Deposit:
                        summary.TotalDepositAmount = stat.Value.TotalAmount;
                        break;
                    case TransactionType.Withdrawal:
                        summary.TotalWithdrawalAmount = stat.Value.TotalAmount;
                        break;
                    case TransactionType.Payment:
                        summary.TotalPaymentAmount = stat.Value.TotalAmount;
                        break;
                    case TransactionType.Refund:
                        summary.TotalRefundAmount = stat.Value.TotalAmount;
                        break;
                    case TransactionType.Commission:
                        summary.TotalCommissionAmount = stat.Value.TotalAmount;
                        break;
                    case TransactionType.Fee:
                        summary.TotalFeeAmount = stat.Value.TotalAmount;
                        break;
                }
            }

            return summary;
        }
    }
    
    public class HeldFundsSummaryDTO
    {
        public decimal TotalHeldAmount { get; set; } // Tổng tiền đang giữ
        public decimal TotalDisputedAmount { get; set; } // Tổng tiền tranh chấp
        public int TotalHeldFundsCount { get; set; } // Tổng số held funds
        public Dictionary<string, decimal> HeldAmountByType { get; set; } = new(); // Số tiền theo loại
        public Dictionary<string, int> HeldCountByType { get; set; } = new(); // Số lượng theo loại
        public Dictionary<string, decimal> HeldAmountByStatus { get; set; } = new(); // Số tiền theo trạng thái
        public Dictionary<string, int> HeldCountByStatus { get; set; } = new(); // Số lượng theo trạng thái

        public static HeldFundsSummaryDTO FromEntities(List<HeldFund> heldFunds)
        {
            var summary = new HeldFundsSummaryDTO
            {
                TotalHeldAmount = heldFunds.Where(h => h.Status == HeldFundStatus.Held).Sum(h => h.Amount),
                TotalDisputedAmount = heldFunds.Where(h => h.Status == HeldFundStatus.Disputed).Sum(h => h.Amount),
                TotalHeldFundsCount = heldFunds.Count
            };

            // Tính theo loại
            var byType = heldFunds
                .GroupBy(h => h.Type)
                .ToDictionary(g => g.Key.ToString(), g => new
                {
                    TotalAmount = g.Sum(h => h.Amount),
                    Count = g.Count()
                });

            foreach (var item in byType)
            {
                summary.HeldAmountByType[item.Key] = item.Value.TotalAmount;
                summary.HeldCountByType[item.Key] = item.Value.Count;
            }

            // Tính theo trạng thái
            var byStatus = heldFunds
                .GroupBy(h => h.Status)
                .ToDictionary(g => g.Key.ToString(), g => new
                {
                    TotalAmount = g.Sum(h => h.Amount),
                    Count = g.Count()
                });

            foreach (var item in byStatus)
            {
                summary.HeldAmountByStatus[item.Key] = item.Value.TotalAmount;
                summary.HeldCountByStatus[item.Key] = item.Value.Count;
            }

            return summary;
        }
    }
}
