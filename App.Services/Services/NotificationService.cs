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
                if (request.Roles.Contains(Role.Staff))
                    userIds.AddRange(_unitOfWork.GetRepository<Staff>().ExistEntities().Select(x => x.UserId));

                if (request.Roles.Contains(Role.Tutor))
                    userIds.AddRange(_unitOfWork.GetRepository<Tutor>().ExistEntities().Select(x => x.UserId));

                if (request.Roles.Contains(Role.Learner))
                    userIds.AddRange(_unitOfWork.GetRepository<Learner>().ExistEntities().Select(x => x.UserId));
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
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var existsUser = await _unitOfWork.GetRepository<AppUser>().ExistEntities()
                .Where(x => request.ReceiverUserIds.All(y => y.Equals(x.Id)))
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
            return response;
        }

        public async Task<List<NotificationResponse>> GetNotificationsOfUserAsync(int page, int size, bool isUnreadOnly)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
            var userId = _userService.GetCurrentUserId();
                Expression<Func<NotificationEntity, bool>> loggedInUserOnlyPredicate = e => e.AppUserNotifications.Any(an => an.AppUserId.Equals(userId));
                Expression<Func<NotificationEntity, bool>> unreadOnlyPredicate = e => e.AppUserNotifications.Any(an => an.ReadAt == null);

                var predicate = PredicateBuilder.New(loggedInUserOnlyPredicate);
                if (isUnreadOnly)
                    predicate.And(unreadOnlyPredicate);

                var entities = await _unitOfWork.GetRepository<NotificationEntity>().ExistEntities()
                .OrderByDescending(x => x.CreatedAt)
                .Where(predicate)
                .Skip((page -1) * size)
                .Take(size)
                .Select(x => new NotificationResponse()
                {
                    Id = x.Id,
                    NotificationPriority = x.NotificationPriority,
                    Title = x.Title,
                    Content = x.Content,
                    AdditionalData = x.AdditionalData,
                    CreatedAt = x.CreatedAt,
                    isRead = x.AppUserNotifications.Any(x => x.ReadAt != null & x.AppUserId.Equals(userId))
                })
                .ToListAsync();
                return entities;
            });
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
                return true;
            });
        }

        public async Task<NotificationSenderResponse> GetSenderByIdAsync(string id)
        {
            var user = await _unitOfWork.GetRepository<AppUser>().ExistEntities().FirstOrDefaultAsync(b => b.Id.Equals(id));
            if (user == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
            return user.ToNotificationSenderResponse();
        }

        public async Task<NotificationSenderResponse> GetTutorSenderByIdAsync(string id)
        {
            var tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.UserId.Equals(id));
            if (tutor == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
            return tutor.ToNotificationSenderResponse();
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
                Parallel.ForEach(receiverUserIds, (userId) =>
                {
                    receivers.Add(new AppUserNotification
                    {
                        AppUserId = userId,
                        NotificationEntityId = entity.Id
                    });
                });
                _unitOfWork.GetRepository<AppUserNotification>().InsertRange(receivers);
                await _unitOfWork.SaveAsync();

                return entity;
            });
            return response.ToResponse();
        }
    }
}
