using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Services.Services
{
    public class TutorApplicationStaffService : ITutorApplicationStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public TutorApplicationStaffService(IUnitOfWork unitOfWork, IUserService userService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<List<TutorApplicationResponse>> GetAllPendingTutorApplicationsAsync(int page, int size)
        {
            var result = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var tutorApplications = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                        .OrderBy(e => e.CreatedTime)
                        .Where(e => e.Status == ApplicationStatus.PendingVerification || e.Status == ApplicationStatus.PendingReverification)
                        .Skip((page - 1) * size)
                        .Take(size)
                        //.Include(e => e.Tutor) //Currently Not Working
                        .ToListAsync();
                var tutorAppResponses = new List<TutorApplicationResponse>();
                foreach (var app in tutorApplications)
                {
                    app.Tutor = await _unitOfWork.GetRepository<Tutor>().GetByIdAsync(app.TutorId);
                    tutorAppResponses.Add(app.ToTutorApplicationResponse());
                }
                return tutorAppResponses;
            });
            return result;
        }

        public async Task<TutorApplicationResponse> GetTutorApplicationByIdAsync(string id)
        {
            //var result = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
            //    .Include(e => e.Tutor)
            //    .Include(e => e.ApplicationRevisions)
            //    .Include(e => e.Documents)
            //    .FirstOrDefaultAsync(e => e.Id.Equals(id));

            //if (result == null)
            //    throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

            var result = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () => //bandaid fix for multiple Include() bug
            {
                var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                //.Include(e => e.Tutor) // currently not working
                //.Include(e => e.ApplicationRevisions)
                //.Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id.Equals(id));
                if (tutorApplication == null)
                    throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

                tutorApplication.Tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId.Equals(tutorApplication.TutorId));

                tutorApplication.ApplicationRevisions = await _unitOfWork.GetRepository<ApplicationRevision>().ExistEntities()
                .Where(e => e.ApplicationId.Equals(tutorApplication.Id)).ToListAsync();

                tutorApplication.Documents = await _unitOfWork.GetRepository<Document>().ExistEntities()
                .Include(e => e.DocumentFileUploads).ThenInclude(e => e.FileUpload)
                .Where(e => e.ApplicationId.Equals(tutorApplication.Id)).ToListAsync();

                return tutorApplication;
            });
            return await result.ToDetailedResponse();
        }

        public async Task<RevisionResponse> CreateApplicationRevisionAsync(ApplicationRevisionCreateRequest request)
        {
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                .FirstOrDefaultAsync(e => e.Id.Equals(request.ApplicationId));
            if (tutorApplication == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

            var entity = request.ToEntity(_userService.GetCurrentUserId());
            _unitOfWork.GetRepository<ApplicationRevision>().Insert(entity);

            switch (request.Action)
            {
                case RevisionAction.Approve:

                    await UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.Verified);
                    await UpdateTutorVerificationStatusAsync(tutorApplication.TutorId, VerificationStatus.Verified);

                    break;
                case RevisionAction.RequestRevision:
                    await UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.RevisionRequested);
                    break;
                case RevisionAction.Reject:
                    await UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.Rejected);
                    await UpdateTutorVerificationStatusAsync(tutorApplication.TutorId, VerificationStatus.Basic);
                    break;
            }
                
            await _unitOfWork.SaveAsync();

            await _notificationService.SendToUsersAsync(new()
            {
                Content = new()
                {
                    NotificationPriority = Repositories.Models.Notifications.ENotificationPriority.Normal,
                    Title = "PUSH_ON_TUTOR_APPLICATION_REVIEWED",
                    Content = "PUSH_ON_TUTOR_APPLICATION_REVIEWED_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Id = entity.Id,
                        RevisionAction = entity.Action.ToString(),
                    })
                },
                ReceiverUserIds = [tutorApplication.TutorId]
            });

            return entity.ToRevisionResponse();
        }

        #region private
        private async Task UpdateApplicationStatusAsync(string tutorApplicationId, ApplicationStatus status)
        {
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                    .FirstOrDefaultAsync(e => e.Id.Equals(tutorApplicationId));
            tutorApplication.Status = status;
            _unitOfWork.GetRepository<TutorApplication>().Update(tutorApplication);
            await _unitOfWork.SaveAsync();
        }

        private async Task UpdateTutorVerificationStatusAsync(string tutorId, VerificationStatus status)
        {
            var tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                .FirstOrDefaultAsync(e => e.UserId.Equals(tutorId));
            if (tutor == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_NOT_FOUND");
            tutor.VerificationStatus = status;
            _unitOfWork.GetRepository<Tutor>().Update(tutor);
            await _unitOfWork.SaveAsync();
        }


        #endregion
    }
}
