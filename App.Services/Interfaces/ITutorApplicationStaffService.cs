using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface ITutorApplicationStaffService
    {
        Task<ApplicationRevision> CreateApplicationRevisionAsync(ApplicationRevisionCreateRequest request);
        Task<List<TutorApplicationResponse>> GetAllPendingTutorApplicationsAsync(int page, int size);
        Task<TutorApplicationResponse> GetTutorApplicationByIdAsync(string id);
    }
}