using App.Repositories.Models.Notifications;
using System.Text.Json.Serialization;

namespace App.DTOs.NotificationDTOs
{
    public class NotificationListResponse
    {
        public ICollection<NotificationResponse> Data { get; set; } = new List<NotificationResponse>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Page { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxPage { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Total {  get; set; }
    }

    public static class NotificationListResponseExtensions
    {
        public static NotificationListResponse ToResponseList(this ICollection<NotificationEntity> entities, int? page, int? size, int? total)
        {
            var response = new NotificationListResponse();

            Parallel.ForEach(entities, entity => {
                response.Data.Add(entity.ToResponse());
            });
            response.Page = page;
            if (size != null && total != null)
                response.MaxPage = (total / size) + ((total% size) > 0 ? 1 : 0);
            response.Total = total;

            return response;
        }
    }

}
