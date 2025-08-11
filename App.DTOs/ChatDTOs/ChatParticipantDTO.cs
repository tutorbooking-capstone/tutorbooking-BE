using App.Repositories.Models.User;

namespace App.DTOs.ChatDTOs
{
	public class ChatParticipantDTO
	{
		public string Id { get; set; }
		public string FullName { get; set; }
		public string ProfilePictureUrl { get; set; } = string.Empty;
	}

	public static class ChatParticipantDTOExtenstions
	{
		public static ChatParticipantDTO ToChatParticipantDTO(this AppUser appUser)
			=> new()
			{
				Id = appUser.Id,
				FullName = appUser.FullName,
				ProfilePictureUrl = appUser.ProfilePictureUrl,
			};

		public static ICollection<ChatParticipantDTO> ToChatParticipantDTOs(this ICollection<AppUser> entities)
		{
			var list = new List<ChatParticipantDTO>();
			foreach (var user in entities)
			{
				list.Add(user.ToChatParticipantDTO());
			}
			return list;
		}
	}
}
