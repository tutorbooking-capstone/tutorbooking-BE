using App.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorIntroductionVideoResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TutorUserId { get; set; } = string.Empty;
        public TutorIntroductionVideoStatus Status { get; set; } = TutorIntroductionVideoStatus.Pending;
        public string Url { get; set; } = string.Empty;
    }

    public static class TutorIntroductionVideoDTOExtensions
    {
        public static TutorIntroductionVideoResponse ToResponse(this TutorIntroductionVideo entity)
        {
            return new TutorIntroductionVideoResponse
            {
                Id = entity.Id,
                TutorUserId = entity.TutorUserId,
                Status = entity.Status,
                Url = entity.Url
            };
        }
    }
}
