using App.Repositories.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ApplicationDTOs.TutorApplicationDTOs
{
    public class ApplicationRevisionCreateRequest
    {
        [Required]
        public string ApplicationId { get; set; }
        [Required]
        public RevisionAction Action { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public static class ApplicationRevisionCreateRequestExtenstions
    {
        public static ApplicationRevision ToEntity(this ApplicationRevisionCreateRequest request, string staffId)
        {
            var entity = new ApplicationRevision();
            entity.ApplicationId = request.ApplicationId;
            entity.StaffId = staffId;
            entity.Action = request.Action;
            entity.Notes = request.Notes;
            return entity;
        }
    }
}
