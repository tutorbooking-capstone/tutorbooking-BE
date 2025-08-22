using App.Repositories.Models;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace App.Services.Hangfire
{
    public class BookingHeldFundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingHeldFundService> _logger;

        public BookingHeldFundService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<BookingHeldFundService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessHeldFundReleaseAsync(string heldFundId)
        {
            try
            {
                var heldFund = await _unitOfWork.GetRepository<HeldFund>()
                    .ExistEntities()
                    .Include(h => h.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                    .FirstOrDefaultAsync(h => h.Id == heldFundId);

                if (heldFund == null)
                {
                    _logger.LogError("Held fund with ID {HeldFundId} not found", heldFundId);
                    return;
                }

                if (heldFund.Status != HeldFundStatus.Held)
                {
                    _logger.LogWarning("Held fund {HeldFundId} is not in 'Held' status, current status: {Status}", 
                        heldFundId, heldFund.Status);
                    return;
                }

                await ReleaseHeldFundToTutorAsync(heldFund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing held fund release for ID {HeldFundId}", heldFundId);
            }
        }

        public async Task ProcessPendingHeldFundsAsync()
        {
            try
            {
                var pendingHeldFunds = await _unitOfWork.GetRepository<HeldFund>()
                    .ExistEntities()
                    .Where(h => h.Status == HeldFundStatus.Held && 
                                h.ReleaseAt.HasValue && 
                                h.ReleaseAt.Value < DateTime.UtcNow)
                    .ToListAsync();
                    
                _logger.LogInformation("Found {Count} held funds pending release", pendingHeldFunds.Count);
                    
                foreach(var fund in pendingHeldFunds)
                {
                    try 
                    {
                        await ProcessHeldFundReleaseAsync(fund.Id);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process held fund {Id}", fund.Id);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing pending held funds");
            }
        }

        private async Task ReleaseHeldFundToTutorAsync(HeldFund heldFund)
        {
            if (heldFund.BookedSlot == null || heldFund.BookedSlot.Booking == null)
            {
                _logger.LogError("Cannot process held fund {HeldFundId} - missing related booking data", heldFund.Id);
                return;
            }

            var booking = heldFund.BookedSlot.Booking;
            var tutorId = booking.TutorId;
            var tutorWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == tutorId);

            var escrowWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.Escrow);

            if (tutorWallet == null || escrowWallet == null)
            {
                _logger.LogError("Cannot process held fund {HeldFundId} - wallets not found", heldFund.Id);
                return;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Create transaction
                var transaction = Transaction.CreatePaymentTransaction(
                    escrowWallet.Id,
                    tutorWallet.Id,
                    heldFund.Amount,
                    booking.Id,
                    $"Payment release from escrow for booking slot {heldFund.BookedSlot.Id}"
                );
                
                _unitOfWork.GetRepository<Transaction>().Insert(transaction);
                
                // Update wallet balances
                var escrowUpdateFields = escrowWallet.SubtractBalance(heldFund.Amount);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
                
                var tutorUpdateFields = tutorWallet.AddBalance(heldFund.Amount);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(tutorWallet, tutorUpdateFields);
                
                // Update held fund status
                var heldFundUpdateFields = heldFund.UpdateStatus(HeldFundStatus.ReleasedToTutor);
                _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, heldFundUpdateFields);
                
                //Vẫn update các Slot
                if (heldFund.BookedSlot.Status == SlotStatus.AwaitingPayout || heldFund.BookedSlot.Status == SlotStatus.Pending)
                {
                    try 
                    {
                        var bookedSlotUpdateFields = heldFund.BookedSlot.ModifySlotStatus("SYSTEM", SlotStatus.Completed);
                        _unitOfWork.GetRepository<BookedSlot>().UpdateFields(heldFund.BookedSlot, bookedSlotUpdateFields);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex.Message);
                    }
                }
                
                await _unitOfWork.SaveAsync();

                // Send notification to tutor
                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "PUSH_ON_PAYMENT_RELEASED",
                        Content = "PUSH_ON_PAYMENT_RELEASED_BODY",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            BookedSlotId = heldFund.BookedSlot.Id,
                            Amount = heldFund.Amount
                        })
                    },
                    ReceiverUserIds = [tutorId]
                });
            });
        }
    }
}