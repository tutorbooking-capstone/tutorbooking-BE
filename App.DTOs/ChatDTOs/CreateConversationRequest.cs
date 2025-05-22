using App.Core.Base;
using App.Repositories.Models;
using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class CreateConversationRequest
	{
		[Length(2,2)]
		public ICollection<string>? ParticipantUserIds { get; set; }
	}
}
