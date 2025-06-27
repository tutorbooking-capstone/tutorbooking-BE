using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
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
            tutorApplication.Status = (tutorApplication.Status == ApplicationStatus.RevisionRequested) ? 
                ApplicationStatus.PendingReverification : ApplicationStatus.PendingVerification;
            _unitOfWork.GetRepository<TutorApplication>().Update(tutorApplication);
            await _unitOfWork.SaveAsync();
        }
    }
} 