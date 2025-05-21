using App.Repositories.Models;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class ChatParticipantDTO
	{
		public string Id { get; set; }
		public string FullName { get; set; }
	}

	public static class ChatParticipantDTOExtenstions
	{
		public static ChatParticipantDTO ToChatParticipantDTO(this AppUser appUser)
			=> new()
			{
				Id = appUser.Id,
				FullName = appUser.FullName,
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
