using App.DTOs.NotificationDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Notifications;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace App.Services.Hangfire
{
    public class OfferExpirationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OfferExpirationService> _logger;

        public OfferExpirationService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<OfferExpirationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessExpiredOffersAsync()
        {
            try
            {
                _logger.LogInformation("Bắt đầu xử lý các offer hết hạn");

                var expiredOffers = await _unitOfWork.GetRepository<TutorBookingOffer>()
                    .ExistEntities()
                    .Where(o => !o.IsRejected && !o.IsExpired)  
                    .Where(o => (o.UpdatedAt ?? o.CreatedAt) < DateTimeOffset.UtcNow - o.ExpirationPeriod)
                    .Include(o => o.OfferedSlots)
                    .ToListAsync();

                foreach (var offer in expiredOffers)
                {
                    try
                    {
                        await HandleExpiredOfferAsync(offer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing expired offer {OfferId}", offer.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired offers");
            }
        }

        public async Task HandleExpiredOfferAsync(TutorBookingOffer offer)
        {
            if (offer == null || offer.IsRejected || offer.IsExpired) return;
            
            // if (!offer.IsExpired)
            //     return;

            var slotRepo = _unitOfWork.GetRepository<OfferedSlot>();
            slotRepo.DeleteRange(offer.OfferedSlots);
            
            var expiredFields = offer.MarkAsExpired();
            _unitOfWork.GetRepository<TutorBookingOffer>().UpdateFields(offer, expiredFields);
            
            await _unitOfWork.SaveAsync();
            //await SendExpirationNotificationsAsync(offer);
        }

        public async Task HandleExpiredOfferByIdAsync(string offerId)
        {
            var offer = await _unitOfWork.GetRepository<TutorBookingOffer>()
                .ExistEntities()
                .Include(o => o.OfferedSlots)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer != null)
                await HandleExpiredOfferAsync(offer);
        }

        private async Task SendExpirationNotificationsAsync(TutorBookingOffer offer)
        {
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "Offer đã hết hạn",
                    Content = $"Offer của bạn cho học viên {offer.Learner?.User?.FullName} đã hết hạn",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "OfferExpired",
                        OfferId = offer.Id
                    })
                },
                ReceiverUserIds = [offer.TutorId]
            });

            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "Offer đã hết hạn",
                    Content = $"Offer từ gia sư {offer.Tutor?.User?.FullName} đã hết hạn",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "OfferExpired",
                        OfferId = offer.Id
                    })
                },
                ReceiverUserIds = [offer.LearnerId]
            });
        }
    }
}
