using App.Core.Base;
using App.Core.Constants;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.UoW;
using App.Services.Interfaces;

namespace App.Services.Services
{
    internal class TutorApplicationService : ITutorApplicationService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public TutorApplicationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        public async Task CreateTutorApplicationAsync(string tutorId)
        {
            var tutorApplication = TutorApplication.Create(tutorId);
            _unitOfWork.GetRepository<TutorApplication>().Insert(tutorApplication);
            await _unitOfWork.SaveAsync();
        }

        public async Task RequestVerificationAsync(string tutorApplicationId)
        {
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().GetByIdAsync(tutorApplicationId);
            if (tutorApplication == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");
            if (tutorApplication.Status == ApplicationStatus.Verified)
                throw new ErrorException((int)StatusCode.ServerError, ErrorCode.ServerError, "TUTOR_APPLICATION_ALREADY_VERIFIED");
            tutorApplication.Status = ApplicationStatus.PendingVerification;
            _unitOfWork.GetRepository<TutorApplication>().Update(tutorApplication);
            await _unitOfWork.SaveAsync();
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
            
            return metadata;
        }
    }
} 