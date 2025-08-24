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
    public class RescheduleExpirationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<RescheduleExpirationService> _logger;

        public RescheduleExpirationService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<RescheduleExpirationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessExpiredRescheduleRequestsAsync()
        {
            try
            {
                _logger.LogInformation("Processing expired reschedule requests");

                var now = DateTime.UtcNow;

                var expiredRequests = await _unitOfWork.GetRepository<RescheduleRequest>()
                    .ExistEntities()
                    .Where(r => r.Status == RescheduleRequestStatus.Pending && 
                                r.ExpiresAt < now)
                    .Include(r => r.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                    .ToListAsync();

                foreach (var request in expiredRequests)
                {
                    try
                    {
                        await HandleExpiredRescheduleRequestAsync(request.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing expired reschedule request {RequestId}", request.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired reschedule requests");
            }
        }

        public async Task HandleExpiredRescheduleRequestAsync(string requestId)
        {
            var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Include(r => r.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != RescheduleRequestStatus.Pending)
                return;

            if (!request.IsExpired())
                return;

            // Update the request status
            var updateFields = request.MarkExpired();
            if (updateFields.Any())
            {
                var offeredSlotRepo = _unitOfWork.GetRepository<OfferedSlot>();
                foreach (var slot in request.OfferedSlots.ToList())
                {
                    offeredSlotRepo.Delete(slot);
                }
                
                _unitOfWork.GetRepository<RescheduleRequest>().UpdateFields(request, updateFields);
                await _unitOfWork.SaveAsync();
                
                // Send notifications to both parties
                var booking = request.BookedSlot?.Booking;
                if (booking != null)
                {
                    // Notify tutor
                    await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                    {
                        Content = new()
                        {
                            NotificationPriority = ENotificationPriority.Normal,
                            Title = "PUSH_ON_RESCHEDULE_EXPIRED",
                            Content = "PUSH_ON_RESCHEDULE_EXPIRED_BODY",
                            AdditionalData = JsonSerializer.Serialize(new
                            {
                                RescheduleRequestId = request.Id,
                                BookedSlotId = request.BookedSlotId,
                                BookingId = booking.Id
                            })
                        },
                        ReceiverUserIds = [booking.TutorId]
                    });

                    // Notify learner if they're not the requester
                    if (booking.LearnerId != null && booking.LearnerId != request.RequestedByUserId)
                    {
                        await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                        {
                            Content = new()
                            {
                                NotificationPriority = ENotificationPriority.Normal,
                                Title = "PUSH_ON_RESCHEDULE_EXPIRED",
                                Content = "PUSH_ON_RESCHEDULE_EXPIRED_BODY",
                                AdditionalData = JsonSerializer.Serialize(new
                                {
                                    RescheduleRequestId = request.Id,
                                    BookedSlotId = request.BookedSlotId,
                                    BookingId = booking.Id
                                })
                            },
                            ReceiverUserIds = [booking.LearnerId]
                        });
                    }
                }
            }
        }
    }
}