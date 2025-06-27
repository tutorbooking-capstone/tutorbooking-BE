using App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.DocumentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.Models.User;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App.DTOs.ApplicationDTOs.TutorApplicationDTOs
{
    public class TutorApplicationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public ApplicationStatus Status { get; set; }
        public string RevisionNotes { get; set; } = string.Empty;
        public string InternalNotes { get; set; } = string.Empty; // Internal notes for administrative use (not shown to tutors)
        public string TutorName { get; set; } = string.Empty;
        public virtual TutorResponse? Tutor { get; set; }
        public virtual ICollection<DocumentResponse>? Documents { get; set; } = new List<DocumentResponse>();
        public virtual ICollection<RevisionResponse>? ApplicationRevisions { get; set; } = new List<RevisionResponse>();


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

        public static async Task<TutorApplicationResponse> ToDetailedResponse(this TutorApplication entity)
        {
            var response = new TutorApplicationResponse();
            response.Id = entity.Id;
            response.TutorId = entity.TutorId;
            response.SubmittedAt = entity.SubmittedAt;
            response.Status = entity.Status;
            response.InternalNotes = entity.InternalNotes;
            response.Tutor = entity.Tutor.ToTutorResponse();

            var task1 = Task.Run(() =>
            {
                if (entity.ApplicationRevisions != null)
                    foreach (var note in entity.ApplicationRevisions)
                        response.ApplicationRevisions.Add(note.ToRevisionResponse());
            });

            var task2 = Task.Run(() =>
            {
                if (entity.Documents != null)
                    foreach (var document in entity.Documents)
                        response.Documents.Add(document.ToDocumentResponse());
            });
            await Task.WhenAll(task1, task2);
            return response;
        }
    }
    #endregion
}
