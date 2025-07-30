using App.Core.Base;
using App.Core.Constants;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Services.Services
{
    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentProcessingService> _logger;
        private readonly decimal _platformFeePercentage = 0.1m; // 10% platform fee
        private readonly BookingSettings _bookingSettings;
        private readonly IWalletService _walletService;


        public PaymentProcessingService(
            IUnitOfWork unitOfWork,
            ILogger<PaymentProcessingService> logger,
            IOptions<BookingSettings> bookingSettings,
            IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _bookingSettings = bookingSettings.Value;
            _walletService = walletService;
        }

        public async Task ProcessHeldFundReleaseAsync(string heldFundId)
        {
            try
            {
                _logger.LogInformation("Processing held fund release for ID: {HeldFundId}", heldFundId);
                
                var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                    .ExistEntities()
                    .Include(h => h.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                    .FirstOrDefaultAsync(h => h.Id == heldFundId);
                
                if (heldFund == null)
                {
                    _logger.LogWarning("Held fund not found: {HeldFundId}", heldFundId);
                    return;
                }
                
                if (heldFund.Status != HeldFundStatus.Held)
                {
                    _logger.LogInformation("Held fund {HeldFundId} is already processed (status: {Status})", heldFundId, heldFund.Status);
                    return;
                }
                
                if (heldFund.BookedSlot == null || heldFund.BookedSlot.Booking == null)
                {
                    _logger.LogWarning("Held fund {HeldFundId} has invalid references", heldFundId);
                    return;
                }
                
                var booking = heldFund.BookedSlot.Booking;
                
                // Check if the booked slot was completed or cancelled
                if (heldFund.BookedSlot.Status == SlotStatus.Completed)
                {
                    await ReleaseToTutor(heldFund, booking.TutorId);
                }
                else if (heldFund.BookedSlot.Status == SlotStatus.Cancelled)
                {
                    if (string.IsNullOrEmpty(booking.LearnerId))
                    {
                        _logger.LogError("Booking {BookingId} associated with held fund {HeldFundId} has a null or empty LearnerId. Cannot process refund.", booking.Id, heldFund.Id);
                        return;
                    }
                    await RefundToLearner(heldFund, booking.LearnerId);
                }
                else
                {
                    // For pending or awaiting confirmation slots, we'll auto-complete them
                    // In a real system, you might want more sophisticated handling here
                    await ReleaseToTutor(heldFund, booking.TutorId);
                    
                    // Update slot status to completed
                    heldFund.BookedSlot.Status = SlotStatus.Completed;
                    _unitOfWork.GetRepository<BookedSlot>().UpdateFields(heldFund.BookedSlot, bs => bs.Status);
                }
                
                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing held fund release for ID: {HeldFundId}", heldFundId);
                throw;
            }
        }
        
        private async Task ReleaseToTutor(HeldFund heldFund, string tutorId)
        {
            // Get escrow wallet
            var escrowWallet = await _walletService.GetEscrowWalletAsync();
            var systemWallet = await GetSystemWalletAsync();
            
            // Get tutor wallet
            var tutorWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == tutorId);
                
            if (tutorWallet == null)
            {
                _logger.LogError("Tutor wallet not found for tutor ID: {TutorId}", tutorId);
                return;
            }
            
            // Calculate platform fee and tutor amount
            var platformFee = heldFund.Amount * _platformFeePercentage;
            var tutorAmount = heldFund.Amount - platformFee;
            
            // Create transactions
            var tutorTransaction = new Transaction
            {
                SourceWalletId = escrowWallet.Id,
                TargetWalletId = tutorWallet.Id,
                Amount = tutorAmount,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Success,
                ReferenceId = heldFund.Id,
                Description = $"Payment for completed session (HeldFund: {heldFund.Id})"
            };
            
            var feeTransaction = new Transaction
            {
                SourceWalletId = escrowWallet.Id,
                TargetWalletId = systemWallet.Id,
                Amount = platformFee,
                Type = TransactionType.Fee,
                Status = TransactionStatus.Success,
                ReferenceId = heldFund.Id,
                Description = $"Platform fee for session (HeldFund: {heldFund.Id})"
            };
            
            _unitOfWork.GetRepository<Transaction>().Insert(tutorTransaction);
            _unitOfWork.GetRepository<Transaction>().Insert(feeTransaction);
            
            var escrowUpdateFields = escrowWallet.SubtractBalance(heldFund.Amount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
            
            var systemUpdateFields = systemWallet.AddBalance(platformFee);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(systemWallet, systemUpdateFields);
            
            var tutorUpdateFields = tutorWallet.AddBalance(tutorAmount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(tutorWallet, tutorUpdateFields);
            
            var heldFundUpdateFields = heldFund.UpdateStatus(HeldFundStatus.ReleasedToTutor);
            _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdateFields);
            
            _logger.LogInformation("Released {Amount} to tutor {TutorId} with {Fee} platform fee", 
                tutorAmount, tutorId, platformFee);
        }
        
        private async Task RefundToLearner(HeldFund heldFund, string learnerId)
        {
            // Get system wallet
            var systemWallet = await GetSystemWalletAsync();
            
            // Get learner wallet
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);
                
            if (learnerWallet == null)
            {
                _logger.LogError("Learner wallet not found for learner ID: {LearnerId}", learnerId);
                return;
            }
            
            // Create refund transaction
            var refundTransaction = new Transaction
            {
                SourceWalletId = systemWallet.Id,
                TargetWalletId = learnerWallet.Id,
                Amount = heldFund.Amount,
                Type = TransactionType.Refund,
                Status = TransactionStatus.Success,
                ReferenceId = heldFund.Id,
                Description = $"Refund for cancelled session (HeldFund: {heldFund.Id})"
            };
            
            _unitOfWork.GetRepository<Transaction>().Insert(refundTransaction);
            
            // Update wallet balances
            var systemUpdateFields = systemWallet.SubtractBalance(heldFund.Amount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(systemWallet, systemUpdateFields);
            
            var learnerUpdateFields = learnerWallet.AddBalance(heldFund.Amount);
            _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdateFields);
            
            // Update held fund status
            var heldFundUpdateFields = heldFund.UpdateStatus(HeldFundStatus.RefundedToLearner);
            _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdateFields);
            
            _logger.LogInformation("Refunded {Amount} to learner {LearnerId}", heldFund.Amount, learnerId);
        }
        
        private async Task<Wallet> GetSystemWalletAsync()
        {
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);
                
            if (systemWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError, 
                    ErrorCode.ServerError, 
                    "System wallet not found");
                
            return systemWallet;
        }
    }
}