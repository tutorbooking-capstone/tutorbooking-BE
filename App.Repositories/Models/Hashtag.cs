using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Hashtag : BaseEntity
    {
        public string Description { get; set; } = string.Empty; //Brief of hashtag
        public int UsageCount { get; set; } = 0; //Count of usage by Tutor
    }

    public class TutorHashtag
    {
        public string TutorId { get; set; } = string.Empty;
        public string HashtagId { get; set; } = string.Empty;

        public virtual Tutor? Tutor { get; set; } 
        public virtual Hashtag? Hashtag { get; set; } 
    }
}
