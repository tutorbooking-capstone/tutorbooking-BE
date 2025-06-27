using App.Core.Base;
using App.Core.Constants;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Services
{
    public class TutorApplicationStaffService : ITutorApplicationStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public TutorApplicationStaffService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
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
            var tutors = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities().
                OrderBy(e => e.CreatedTime)
                .Where(e => e.Status == ApplicationStatus.PendingVerification || e.Status == ApplicationStatus.PendingReverification)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            var tutorResponses = new List<TutorApplicationResponse>();
            foreach (var tutor in tutors)
            {
                tutorResponses.Add(tutor.ToTutorApplicationResponse());
            }
            return tutorResponses;
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
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                .Include(e => e.Tutor)
                .Include(e => e.ApplicationRevisions)
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id.Equals(id));
            if (tutorApplication == null)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

            return await tutorApplication.ToDetailedResponse();
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
        public async Task<ApplicationRevision> CreateApplicationRevisionAsync(ApplicationRevisionCreateRequest request)
        {
            var existsTutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                .AnyAsync(e => e.Id.Equals(request.ApplicationId));
            if (!existsTutorApplication)
                throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "TUTOR_APPLICATION_NOT_FOUND");

            var entity = request.ToEntity(_userService.GetCurrentUserId());
            _unitOfWork.GetRepository<ApplicationRevision>().Insert(entity);
            if (request.Action == RevisionAction.Approve) 
                UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.Verified);
            if (request.Action == RevisionAction.RequestRevision || request.Action == RevisionAction.Reject) 
                UpdateApplicationStatusAsync(request.ApplicationId, ApplicationStatus.RevisionRequested);
            await _unitOfWork.SaveAsync();       
            return entity;
        }

        #region private
        private async Task UpdateApplicationStatusAsync(string tutorApplicationId, ApplicationStatus status)
        {
            var tutorApplication = await _unitOfWork.GetRepository<TutorApplication>().ExistEntities()
                    .FirstOrDefaultAsync(e => e.Id.Equals(tutorApplicationId));
            tutorApplication.Status = status;
            _unitOfWork.GetRepository<TutorApplication>().Update(tutorApplication);
        }
        #endregion
    }
}
