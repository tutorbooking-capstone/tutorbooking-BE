using App.Core.Base;
using App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.Repositories.Models.Papers;

namespace App.Services.Interfaces
{
    public interface ITutorApplicationStaffService
    {
        Task<RevisionResponse> CreateApplicationRevisionAsync(ApplicationRevisionCreateRequest request);
        Task<List<TutorApplicationResponse>> GetAllPendingTutorApplicationsAsync(int page, int size);
        Task<TutorApplicationResponse> GetTutorApplicationByIdAsync(string id);
        Task<Dictionary<string, object>> GetApplicationMetadataAsync();
        Task<BasePaginatedList<TutorApplicationResponse>> GetAllTutorApplicationsAsync(ApplicationStatus? applicationStatus, int page, int size);
    }
}