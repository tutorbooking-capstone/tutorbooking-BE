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

        /// <summary>
        /// Retrieves a paginated list of tutor applications that are pending verification or re-verification.
        /// </summary>
        /// <remarks>This method retrieves tutor applications with <see
        /// cref="ApplicationStatus.PendingVerification"/> or <see cref="ApplicationStatus.PendingReverification"/> status, sorted by CreatedTime in ascending order.</remarks>
        /// <param name="page">The page number to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="size">The number of items to include in each page. Must be greater than or equal to 1.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="TutorApplicationResponse"/> objects representing the pending tutor applications.</returns>
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

        /// <summary>
        /// Retrieves a tutor application by its unique identifier.
        /// </summary>
        /// <remarks>This method queries the data source for a tutor application and includes related
        /// entities such as the tutor, application revisions, and associated documents. Ensure the <paramref
        /// name="id"/> provided is valid and corresponds to an existing tutor application.</remarks>
        /// <param name="id">The unique identifier of the tutor application to retrieve. Cannot be null or empty.</param>
        /// <returns>A <see cref="TutorApplicationResponse"/> object containing the details of the tutor application.</returns>
        /// <exception cref="ErrorException">Thrown if the tutor application with the specified <paramref name="id"/> is not found.</exception>
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
                    .FirstOrDefaultAsync(e => e.Id.Equals(id));
                if (tutorApplication == null)
                    throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

                tutorApplication.Tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                    .Include(e => e.User)
                    .Include(e => e.Languages) // Thêm include cho Languages
                    .Include(e => e.Hashtags) // Thêm include cho Hashtags
                        .ThenInclude(h => h.Hashtag) // Include thêm Hashtag entity
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

        /// <summary>
        /// Creates a new application revision based on the provided request.
        /// </summary>
        /// <remarks>This method validates the existence of the associated tutor application before
        /// creating the revision. If the specified application does not exist, an <see cref="ErrorException"/> is
        /// thrown. Additionally, if the revision action is set to <see cref="RevisionAction.Approve"/>, the associated
        /// application is automatically approved.</remarks>
        /// <param name="request">The request containing the details for creating the application revision, including the associated
        /// application ID and the desired revision action.</param>
        /// <returns>The newly created <see cref="ApplicationRevision"/> entity.</returns>
        /// <exception cref="ErrorException">Thrown if the tutor application specified in <paramref name="request"/> does not exist.</exception>
        public async Task<RevisionResponse> CreateApplicationRevisionAsync(ApplicationRevisionCreateRequest request)
        {
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                .FirstOrDefaultAsync(e => e.Id.Equals(request.ApplicationId));
            if (tutorApplication == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

            var entity = request.ToEntity(_userService.GetCurrentUserId());
            _unitOfWork.GetRepository<ApplicationRevision>().Insert(entity);
            if (request.Action == RevisionAction.Approve)
            {
                await UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.Verified);

                // Update the tutor's status to verified if the application is approved
                var tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                    .FirstOrDefaultAsync(e => e.UserId.Equals(tutorApplication.TutorId));
                if (tutor != null && tutor.VerificationStatus != VerificationStatus.Verified)
                {
                    tutor.VerificationStatus = VerificationStatus.Verified;
                    _unitOfWork.GetRepository<TutorApplication>().Update(tutorApplication);
                }
            }    
            else if (request.Action == RevisionAction.RequestRevision || request.Action == RevisionAction.Reject)
            {
                await UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.RevisionRequested);
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
        public async Task<Dictionary<string, object>> GetApplicationMetadataAsync()
        {
            var metadata = new Dictionary<string, object>();
            
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(ApplicationStatus),
                typeof(RevisionAction),
                typeof(HardcopySubmitStatus)
            );
            
            foreach (var kv in enumMetadata)
            {
                metadata.Add(kv.Key, kv.Value);
            }
            
            var applicationProcess = new
            {
                UnSubmitted = "Hồ sơ đã được tạo nhưng chưa gửi cho hệ thống xác minh",
                PendingVerification = "Hồ sơ đã gửi và đang chờ nhân viên xác minh",
                RevisionRequested = "Nhân viên yêu cầu chỉnh sửa hồ sơ",
                PendingReverification = "Hồ sơ đã chỉnh sửa và đang chờ xác minh lại",
                Verified = "Hồ sơ đã được xác minh thành công"
            };
            metadata.Add("ApplicationProcess", applicationProcess);
            
            var revisionActions = new
            {
                Approve = "Phê duyệt hồ sơ và xác minh gia sư",
                RequestRevision = "Yêu cầu gia sư chỉnh sửa hồ sơ",
                Reject = "Từ chối hồ sơ"
            };
            metadata.Add("RevisionActions", revisionActions);
            
            var hardcopyStatuses = new
            {
                Pending = "Hồ sơ giấy đang chờ xử lý",
                Processing = "Hồ sơ giấy đang được xem xét",
                Verified = "Hồ sơ giấy đã được xác minh",
                Rejected = "Hồ sơ giấy đã bị từ chối"
            };
            metadata.Add("HardcopyStatuses", hardcopyStatuses);
            
            return metadata;
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
        #endregion
    }
}
