using App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.DocumentDTOs;
using App.Repositories.Models.Papers;
using System.Text.Json.Serialization;

namespace App.DTOs.ApplicationDTOs.TutorApplicationDTOs
{
    public class TutorLanguageResponse
    {
        public string LanguageCode { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int Proficiency { get; set; }
    }

    public class TutorApplicationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public ApplicationStatus Status { get; set; }
        public string RevisionNotes { get; set; } = string.Empty;
        public string InternalNotes { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        
        public List<TutorLanguageResponse> Languages { get; set; } = new List<TutorLanguageResponse>();
        public List<string> Hashtags { get; set; } = new List<string>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public virtual TutorResponse? Tutor { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public virtual ICollection<DocumentResponse>? Documents { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public virtual ICollection<RevisionResponse>? ApplicationRevisions { get; set; }
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
                TutorName = entity.Tutor?.User?.FullName ?? "N/A",
                Tutor = entity.Tutor == null ? null : entity.Tutor.ToTutorResponse(),
                Languages = new List<TutorLanguageResponse>(),
                Hashtags = new List<string>()
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
            response.Tutor = entity.Tutor == null ? null : entity.Tutor.ToTutorResponse();

            var task1 = Task.Run(() =>
            {
                if (entity.ApplicationRevisions != null)
                {
                    response.ApplicationRevisions = new List<RevisionResponse>();
                    foreach (var note in entity.ApplicationRevisions)
                        response.ApplicationRevisions.Add(note.ToRevisionResponse());
                }
            });

            var task2 = Task.Run(() =>
            {
                if (entity.Documents != null)
                {
                    response.Documents = new List<DocumentResponse>();
                    foreach (var document in entity.Documents)
                        response.Documents.Add(document.ToDocumentResponse());
                }
            });

            var task3 = Task.Run(() =>
            {
                if (entity.Tutor != null && entity.Tutor.Languages != null)
                {
                    response.Languages = entity.Tutor.Languages
                        .Select(l => new TutorLanguageResponse
                        {
                            LanguageCode = l.LanguageCode,
                            IsPrimary = l.IsPrimary,
                            Proficiency = l.Proficiency
                        })
                        .ToList();
                }
            });

            var task4 = Task.Run(() =>
            {
                if (entity.Tutor != null && entity.Tutor.Hashtags != null)
                {
                    response.Hashtags = entity.Tutor.Hashtags
                        .Select(h => h.Hashtag?.Name ?? string.Empty)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();
                }
            });

            await Task.WhenAll(task1, task2, task3, task4);
            return response;
        }
    }
    #endregion
}
