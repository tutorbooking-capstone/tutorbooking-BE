using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Hashtag : CoreEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Brief description of the hashtag
        public int UsageCount { get; set; } = 0; // Count of usage by Tutors

    }

    #region Many-to-Many Relationships
    public class TutorHashtag
    {
        public string TutorId { get; set; } = string.Empty;
        public string HashtagId { get; set; } = string.Empty;

        public virtual Tutor? Tutor { get; set; } 
        public virtual Hashtag? Hashtag { get; set; } 
    }
    #endregion

    #region Seed
    public static class HashtagSeeder
    {
        public static List<Hashtag> SeedHashtags()
        {
            var hashtags = new List<Hashtag>
            {
                CreateHashtag("IELTS 7.0", "Chứng chỉ IELTS band 7.0", 18),
                CreateHashtag("IELTS 6.5", "Chứng chỉ IELTS band 6.5", 25),
                CreateHashtag("IELTS 6.0", "Chứng chỉ IELTS band 6.0", 22),
                CreateHashtag("TOEIC 800+", "Chứng chỉ TOEIC 800 điểm trở lên", 15),
                CreateHashtag("TOEIC 700+", "Chứng chỉ TOEIC 700 điểm trở lên", 19),
                CreateHashtag("HSK 5", "Chứng chỉ Hán ngữ HSK cấp 5", 10),
                CreateHashtag("HSK 4", "Chứng chỉ Hán ngữ HSK cấp 4", 12),
                CreateHashtag("JLPT N3", "Chứng chỉ năng lực tiếng Nhật N3", 8),
                CreateHashtag("JLPT N4", "Chứng chỉ năng lực tiếng Nhật N4", 11),
                CreateHashtag("TOPIK 4", "Chứng chỉ năng lực tiếng Hàn TOPIK cấp 4", 7)
            };

            return hashtags;
        }

        private static Hashtag CreateHashtag(
            string name,
            string description,
            int usageCount)
        {
            var obj = new Hashtag
            {
                Name = name,
                Description = description,
                UsageCount = usageCount
            };

            var seedId = obj.SeedId(x => x.Name);
            obj.Id = seedId;

            return obj;
        }

    }
    #endregion
}
