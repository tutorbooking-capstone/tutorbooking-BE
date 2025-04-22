using App.Repositories.Models;
using System;

namespace App.DTOs.ApplicationDTOs.TutorApplicationDTOs
{
    public class TutorApplicationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public ApplicationStatus Status { get; set; }
        public string RevisionNotes { get; set; } = string.Empty; 
        public string TutorName { get; set; } = string.Empty; 
    }

    #region Mapping
    public static class TutorApplicationResponseExtensions
    {
        public static TutorApplicationResponse ToTutorApplicationResponse(this TutorApplication entity)
        {
            return new TutorApplicationResponse
            {
                Id = entity.Id,
                TutorId = entity.TutorId,
                SubmittedAt = entity.SubmittedAt,
                Status = entity.Status,
                RevisionNotes = entity.RevisionNotes,
                TutorName = entity.Tutor?.User?.FullName ?? "N/A" 
            };
        }
    }
    #endregion
}
