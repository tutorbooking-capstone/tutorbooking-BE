using App.Core.Base;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserService _userService;

        public NotificationService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }


        public async Task<NotificationResponse> CreateForRolesAsync(SendNotificationToRolesRequest request)
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
            return response;
        }

        public async Task<NotificationResponse> CreateForUsersAsync(SendNotificationToUsersRequest request)
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
