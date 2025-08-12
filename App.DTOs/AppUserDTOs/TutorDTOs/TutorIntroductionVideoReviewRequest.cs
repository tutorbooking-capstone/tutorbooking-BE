using App.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorIntroductionVideoReviewRequest
    {
        public string Id { get; set; }
        public TutorIntroductionVideoStatus Status{ get; set; }
    }

    public static class TutorIntroductionVideoApprovalRequestExtensions
    {
        public static void Review(this TutorIntroductionVideo entity, ref TutorIntroductionVideoReviewRequest request)
        {
            entity.Status = request.Status;
        }
    }
}
