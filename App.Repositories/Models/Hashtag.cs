using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Hashtag : BaseEntity
    {
        //Brief of hashtag
        public string Description { get; set; } = string.Empty;

        //Count of usage by Tutor
        public int UsageCount { get; set; } = 0;
    }

    public class TutorHashtag : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string HashtagId { get; set; } = string.Empty;

        public virtual Tutor Tutor { get; set; } = new();
        public virtual Hashtag Hashtag { get; set; } = new();
    }
}
