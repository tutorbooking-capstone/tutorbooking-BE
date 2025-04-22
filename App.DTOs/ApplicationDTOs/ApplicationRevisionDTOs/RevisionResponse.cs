using App.Repositories.Models;
using System;

namespace App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs
{
    public class RevisionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty; 
        public RevisionAction Status { get; set; }
        public string RevisionNotes { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
    }

    #region Mapping
    public static class RevisionResponseExtensions
    {
        public static RevisionResponse ToRevisionResponse(this ApplicationRevision entity)
        {
            return new RevisionResponse
            {
                Id = entity.Id,
                ApplicationId = entity.ApplicationId,
                StaffId = entity.StaffId,
                StaffName = entity.Staff?.User?.FullName ?? "N/A", 
                Status = entity.Action, 
                RevisionNotes = entity.Notes,  
                CreatedTime = entity.Timestamp
            };
        }
    }
    #endregion
}