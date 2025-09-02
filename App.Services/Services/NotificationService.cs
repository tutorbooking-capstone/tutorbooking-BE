using App.Core.Base;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Events;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace App.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly NotificationEvents _notificationEvents;
        
        // Cache cho notifications để giảm database queries
        private static readonly Dictionary<string, (DateTime Timestamp, List<NotificationResponse> Data)> _notificationsCache = new();
        private static readonly object _cacheLock = new object();
        private const int CACHE_EXPIRY_SECONDS = 30;
        
        // Batch size cho việc xử lý notifications
        private const int BATCH_SIZE = 100;

        public NotificationService(IUnitOfWork unitOfWork, IUserService userService, NotificationEvents notificationEvents)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _notificationEvents = notificationEvents;
        }

        public async Task<NotificationResponse> SendToRolesAsync(SendNotificationToRolesRequest request)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var userIds = new List<string>();
                
                // Tối ưu query để không load tất cả users vào memory
                if (request.Roles.Contains(Role.Staff))
                    userIds.AddRange(await _unitOfWork.GetRepository<Staff>().ExistEntities()
                        .Select(x => x.UserId)
                        .Take(BATCH_SIZE) // Giới hạn số lượng users để tránh overload
                        .ToListAsync());

                if (request.Roles.Contains(Role.Tutor))
                    userIds.AddRange(await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                        .Select(x => x.UserId)
                        .Take(BATCH_SIZE) // Giới hạn số lượng users để tránh overload
                        .ToListAsync());

                if (request.Roles.Contains(Role.Learner))
                    userIds.AddRange(await _unitOfWork.GetRepository<Learner>().ExistEntities()
                        .Select(x => x.UserId)
                        .Take(BATCH_SIZE) // Giới hạn số lượng users để tránh overload
                        .ToListAsync());
                        
                // Loại bỏ các userId trùng lặp
                userIds = userIds.Distinct().ToList();
                
                var response = await CreateNotificationAsync(request.Content, userIds);
                return response;
            });

            _notificationEvents.RequestSendNotificationToRoles(this, new NotificationToRolesEventArgs()
            {
                NotificationResponse = response,
                Roles = request.Roles
            });

            return response;
        }

        public async Task<NotificationResponse> SendToUsersAsync(SendNotificationToUsersRequest request)
        {
            // Giới hạn số lượng users để tránh overload
            if (request.ReceiverUserIds.Count > BATCH_SIZE)
            {
                request.ReceiverUserIds = request.ReceiverUserIds.Take(BATCH_SIZE).ToList();
            }

            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var existsUser = await _unitOfWork.GetRepository<AppUser>().ExistEntities()
                    .Where(x => request.ReceiverUserIds.Contains(x.Id))
                    .AnyAsync();
                    
                if (!existsUser)
                    throw new ErrorException((int) StatusCode.NotFound, ErrorCode.NotFound, "USER_NOT_FOUND");

                var response = await CreateNotificationAsync(request.Content, request.ReceiverUserIds);
                return response;
            });

            _notificationEvents.RequestSendNotificationToUsers(this, new NotificationToUsersEventArgs()
            {
                NotificationResponse = response,
                ReceiverUserIds = request.ReceiverUserIds
            });
            
            // Xóa cache liên quan
            ClearNotificationsCache(request.ReceiverUserIds);
            
            return response;
        }

        public async Task<List<NotificationResponse>> GetNotificationsOfUserAsync(int page, int size, bool isUnreadOnly)
        {
            var userId = _userService.GetCurrentUserId();
            
            // Kiểm tra cache trước
            string cacheKey = $"notifications_{userId}_{page}_{size}_{isUnreadOnly}";
            lock (_cacheLock)
            {
                if (_notificationsCache.TryGetValue(cacheKey, out var cachedData))
                {
                    if ((DateTime.UtcNow - cachedData.Timestamp).TotalSeconds < CACHE_EXPIRY_SECONDS)
                    {
                        return cachedData.Data;
                    }
                    _notificationsCache.Remove(cacheKey);
                }
            }

            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var predicate = PredicateBuilder.New<NotificationEntity>(true);
                if (isUnreadOnly)
                    predicate.And(e => e.AppUserNotifications.Any(an => an.ReadAt == null && an.AppUserId.Equals(userId)));
                else
                    predicate.And(e => e.AppUserNotifications.Any(an => an.AppUserId.Equals(userId)));

                // Tối ưu query để giảm memory usage
                var entities = await _unitOfWork.GetRepository<NotificationEntity>().ExistEntities()
                    .OrderByDescending(x => x.CreatedAt)
                    .Where(predicate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(x => new NotificationResponse()
                    {
                        Id = x.Id,
                        NotificationPriority = x.NotificationPriority,
                        Title = x.Title.Length > 100 ? x.Title.Substring(0, 100) : x.Title, // Giới hạn kích thước
                        Content = x.Content.Length > 500 ? x.Content.Substring(0, 500) : x.Content, // Giới hạn kích thước
                        AdditionalData = x.AdditionalData,
                        CreatedAt = x.CreatedAt,
                        isRead = x.AppUserNotifications.Any(x => x.ReadAt != null && x.AppUserId.Equals(userId))
                    })
                    .ToListAsync();
                    
                return entities;
            });
            
            // Lưu vào cache
            lock (_cacheLock)
            {
                _notificationsCache[cacheKey] = (DateTime.UtcNow, response);
                
                // Dọn cache nếu quá lớn
                if (_notificationsCache.Count > 100)
                {
                    var oldestKey = _notificationsCache
                        .OrderBy(x => x.Value.Timestamp)
                        .First().Key;
                    _notificationsCache.Remove(oldestKey);
                }
            }
            
            return response;
        }
        
        public async Task MarkAsReadAsync(string notificationId, string userId)
        {
            await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = await _unitOfWork.GetRepository<AppUserNotification>().ExistEntities()
                    .FirstOrDefaultAsync(x => x.NotificationEntityId.Equals(notificationId) && x.AppUserId.Equals(userId));
                    
                if (entity == null)
                    throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "NOT_FOUND");
                    
                entity.ReadAt = DateTime.UtcNow;
                _unitOfWork.GetRepository<AppUserNotification>().Update(entity);
                await _unitOfWork.SaveAsync();
                
                // Xóa cache liên quan
                ClearNotificationsCache(userId);
                
                return true;
            });
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                // Sử dụng batch update thay vì load tất cả entities
                var count = await _unitOfWork.GetRepository<AppUserNotification>().ExistEntities()
                    .Where(x => x.AppUserId.Equals(userId) && x.ReadAt == null)
                    .Take(BATCH_SIZE) // Giới hạn số lượng để tránh overload
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReadAt, DateTime.UtcNow));
                    
                if (count == 0)
                    throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "NOT_FOUND");
                    
                // Xóa cache liên quan
                ClearNotificationsCache(userId);
                
                return true;
            });
        }

        public async Task<NotificationSenderResponse> GetSenderByIdAsync(string id)
        {
            var user = await _unitOfWork.GetRepository<AppUser>().ExistEntities()
                .Select(u => new { u.Id, u.FullName, u.ProfilePictureUrl })
                .FirstOrDefaultAsync(b => b.Id.Equals(id));
                
            if (user == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
                
            return new NotificationSenderResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<NotificationSenderResponse> GetTutorSenderByIdAsync(string id)
        {
            var tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .Include(b => b.User)
                .Select(t => new { t.UserId, t.User.FullName, t.User.ProfilePictureUrl })
                .FirstOrDefaultAsync(b => b.UserId.Equals(id));
                
            if (tutor == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
                
            return new NotificationSenderResponse
            {
                Id = tutor.UserId,
                FullName = tutor.FullName,
                ProfilePictureUrl = tutor.ProfilePictureUrl
            };
        }

        private async Task<NotificationResponse> CreateNotificationAsync(NotificationRequest request, ICollection<string> receiverUserIds)
        {
            if (receiverUserIds.Count == 0)
                throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "REQUIRES_AT_LEAST_1_RECEIVER");

            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = request.ToEntity();
                _unitOfWork.GetRepository<NotificationEntity>().Insert(entity);
                await _unitOfWork.SaveAsync();

                var receivers = new List<AppUserNotification>();
                // Sử dụng foreach thông thường thay vì Parallel.ForEach để tránh race condition
                foreach (var userId in receiverUserIds)
                {
                    receivers.Add(new AppUserNotification
                    {
                        AppUserId = userId,
                        NotificationEntityId = entity.Id
                    });
                    
                    // Batch insert để tránh memory pressure
                    if (receivers.Count >= 100)
                    {
                        _unitOfWork.GetRepository<AppUserNotification>().InsertRange(receivers);
                        await _unitOfWork.SaveAsync();
                        receivers.Clear();
                    }
                }
                
                // Insert các receivers còn lại
                if (receivers.Any())
                {
                    _unitOfWork.GetRepository<AppUserNotification>().InsertRange(receivers);
                    await _unitOfWork.SaveAsync();
                }

                return entity;
            });
            
            return response.ToResponse();
        }
        
        // Helper method để xóa cache liên quan đến user
        private void ClearNotificationsCache(string userId)
        {
            lock (_cacheLock)
            {
                var keysToRemove = _notificationsCache.Keys
                    .Where(k => k.StartsWith($"notifications_{userId}_"))
                    .ToList();
                    
                foreach (var key in keysToRemove)
                {
                    _notificationsCache.Remove(key);
                }
            }
        }
        
        // Helper method để xóa cache liên quan đến nhiều users
        private void ClearNotificationsCache(ICollection<string> userIds)
        {
            foreach (var userId in userIds)
            {
                ClearNotificationsCache(userId);
            }
        }
    }
}
