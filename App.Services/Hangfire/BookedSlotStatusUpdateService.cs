using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace App.Services.Hangfire
{
    public class BookedSlotStatusUpdateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookedSlotStatusUpdateService> _logger;

        public BookedSlotStatusUpdateService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<BookedSlotStatusUpdateService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessCompletedSlotsAsync()
        {
            try
            {
                _logger.LogInformation("Bắt đầu xử lý cập nhật trạng thái các slot đã kết thúc");

                var now = DateTime.UtcNow;

                var completedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                    .ExistEntities()
                    .Include(bs => bs.Booking)
                    .Where(bs => bs.Status == SlotStatus.Pending &&
                            bs.BookedDate.Date.AddMinutes((bs.SlotIndex + 1) * 30) < now)
                    .ToListAsync();

                _logger.LogInformation("Tìm thấy {Count} slot đã kết thúc cần cập nhật", completedSlots.Count);

                foreach (var slot in completedSlots)
                {
                    try
                    {
                        var updateFields = slot.UpdateStatus(SlotStatus.AwaitingPayout, "SYSTEM");
                        _unitOfWork.GetRepository<BookedSlot>().UpdateFields(slot, updateFields);

                        await NotifySlotCompletedAsync(slot);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi cập nhật slot {SlotId}", slot.Id);
                    }
                }

                await _unitOfWork.SaveAsync();
                _logger.LogInformation("Hoàn tất xử lý cập nhật trạng thái các slot");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý cập nhật trạng thái các slot");
            }
        }

        public async Task ProcessSpecificSlotAsync(string slotId)
        {
            try
            {
                var slot = await _unitOfWork.GetRepository<BookedSlot>()
                    .ExistEntities()
                    .Include(bs => bs.Booking)
                    .FirstOrDefaultAsync(bs => bs.Id == slotId);

                if (slot == null)
                {
                    _logger.LogWarning("Không tìm thấy slot với ID {SlotId}", slotId);
                    return;
                }

                if (slot.Status != SlotStatus.Pending)
                {
                    _logger.LogInformation("Slot {SlotId} không ở trạng thái Pending (hiện tại: {Status})", slotId, slot.Status);
                    return;
                }

                var updateFields = slot.UpdateStatus(SlotStatus.AwaitingPayout, "SYSTEM");
                _unitOfWork.GetRepository<BookedSlot>().UpdateFields(slot, updateFields);
                await _unitOfWork.SaveAsync();

                await NotifySlotCompletedAsync(slot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái slot {SlotId}", slotId);
            }
        }

        private async Task NotifySlotCompletedAsync(BookedSlot slot)
        {
            if (slot.Booking == null) return;

            var notificationData = new
            {
                Type = "SlotCompleted",
                BookedSlotId = slot.Id,
                BookingId = slot.BookingId,
                BookedDate = slot.BookedDate,
                SlotIndex = slot.SlotIndex
            };

            await _notificationService.SendToUsersAsync(new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = $"Slot đã hoàn thành",
                    Content = $"Slot {slot.GetSlotStartTime:hh:mm:ss} ngày {slot.BookedDate:dd/MM/yyyy} đã hoàn thành",
                    AdditionalData = JsonSerializer.Serialize(notificationData)
                },
                ReceiverUserIds = [slot.Booking.TutorId]
            });

            if (!string.IsNullOrEmpty(slot.Booking.LearnerId))
            {
                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = $"Slot đã hoàn thành",
                        Content = $"Slot {slot.GetSlotStartTime:hh:mm:ss} ngày {slot.BookedDate:dd/MM/yyyy} đã hoàn thành",
                        AdditionalData = JsonSerializer.Serialize(notificationData)
                    },
                    ReceiverUserIds = [slot.Booking.LearnerId]
                });
            }
        }

        private DateTime CalculateSlotEndTime(DateTime date, int slotIndex)
        {
            return date.Date.AddMinutes((slotIndex + 1) * 30);
        }
    }
}