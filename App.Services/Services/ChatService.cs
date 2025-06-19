using App.Core.Base;
using App.Core.Constants;
using App.Core.Utils;
using App.DTOs.ChatDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Chat;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Services
{
	public class ChatService : IChatService
	{
		private IUnitOfWork _unitOfWork;

		public ChatService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		/// <summary>
		/// Gets all chat conversations that a specific UserId joined (not ChatParticipantId)
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task<ICollection<ChatConversationDTO>> GetConversationsByUserIdAsync(string userId, int page, int size)
		{
			var response = new List<ChatConversationDTO>();
			var chatConversations = await _unitOfWork.GetRepository<ChatConversation>()
				.GetQueryable()
				.OrderByDescending(c => c.CreatedTime)
				.Include(c => c.ChatMessages.OrderByDescending(c => c.CreatedTime).Take(10))
				.Include(c => c.AppUsers)
                .Include(c => c.ChatConversationReadStatus)
                .Where(e => e.AppUsers.Any(x => x.Id.Equals(userId)))
				.Skip((page-1) * size)
				.Take(size)
				.ToListAsync();
			foreach (var conversation in chatConversations)
			{
				response.Add(await conversation.ToChatConversationDTO());
			}
			return response;
		}

		/// <summary>
		/// Returns a ChatConversationDTO by its Id
		/// </summary>
		/// <param name="id"></param>
		/// <param name="page"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		/// <exception cref="ErrorException"></exception>
		public async Task<ChatConversationDTO> GetConversationAsync(string id, int page, int size)
		{
			var conversation = await _unitOfWork.GetRepository<ChatConversation>().GetQueryable()
				.Include(c => c.ChatMessages.OrderByDescending(c => c.CreatedTime).Skip((page - 1) * size).Take(20))
				.Include(c => c.AppUsers)
				.Include(c => c.ChatConversationReadStatus)
				.FirstOrDefaultAsync(e => e.Id.Equals(id));
			if (conversation == null)
				throw new ErrorException(404, ErrorCode.NotFound, "USER_NOT_FOUND");
			return await conversation.ToChatConversationDTO();
		}

		/// <summary>
		/// Creates a new message. If there's no conversation corresponding to the uyser
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		/// <exception cref="ErrorException"></exception>
		public async Task<ChatMessageDTO> SendMessageAsync(SendMessageRequest request)
		{
			if (request.SenderUserId.Equals(request.ReceiverUserId))
                throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "SENDER_ID_SAME_AS_RECEIVER_ID");

            var conversation = await _unitOfWork.GetRepository<ChatConversation>().ExistEntities()
				.FirstOrDefaultAsync(e => e.AppUsers.Any(x => x.Id.Equals(request.SenderUserId)
						&& e.AppUsers.Any(x => x.Id.Equals(request.ReceiverUserId)
						)));
			if(conversation == null)
				conversation = await CreateConversation(new CreateConversationRequest()
				{
					ParticipantUserIds = new[] { request.SenderUserId, request.ReceiverUserId },
				});

			var message = new ChatMessage()
			{
				AppUserId = request.SenderUserId,
				TextMessage = request.TextMessage,
				ChatConversationId = conversation.Id,
			};
			_unitOfWork.GetRepository<ChatMessage>().Insert(message);
			await _unitOfWork.SaveAsync();
			return message.ToChatMessageDTO();
		}

		public async Task<ChatConversation?> CreateConversation(CreateConversationRequest request)
		{
			var conversation = new ChatConversation()
			{
				AppUsers = new List<AppUser>(),
				ChatMessages = new List<ChatMessage>()
			};

			var users = await _unitOfWork.GetRepository<AppUser>().ExistEntities()
				.Where(x => request.ParticipantUserIds.Contains(x.Id))
				.ToListAsync();
			if (users.Count() != 2)
				throw new ErrorException(404, ErrorCode.NotFound, "USER_NOT_FOUND");

			conversation.AppUsers = users;
			_unitOfWork.GetRepository<ChatConversation>().Insert(conversation);
			await _unitOfWork.SaveAsync();
			return conversation;
		}

		public async Task<ChatMessageDTO> UpdateMessageAsync(UpdateMessageRequest request)
		{
			var message = await _unitOfWork.GetRepository<ChatMessage>().ExistEntities()
				.FirstOrDefaultAsync(x => x.Id.Equals(request.Id));
			if (message == null)
				throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "CHAT_MESSAGE_NOT_FOUND");

			message.TextMessage = request.TextMessage;
			_unitOfWork.GetRepository<ChatMessage>().Update(message);
			await _unitOfWork.SaveAsync();
			return message.ToChatMessageDTO();
		}

        public async Task DeleteMessageAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<ChatMessage>().GetByIdAsync(id);
			_unitOfWork.GetRepository<ChatMessage>().Delete(entity);
			await _unitOfWork.SaveAsync();
        }

		public async Task MarkAsReadAsync(string userId, string messageId)
		{
			var message = await _unitOfWork.GetRepository<ChatMessage>().ExistEntities()
				.FirstOrDefaultAsync(e => e.Id.Equals(messageId) && e.AppUserId.Equals(userId));
			if (message == null)
				throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "MESSAGE_NOT_FOUND");

			var entity = await _unitOfWork.GetRepository<ChatConversationReadStatus>().ExistEntities()
				.FirstOrDefaultAsync(e => e.ChatConversationId.Equals(message.ChatConversationId) && e.UserId.Equals(userId));

			if (entity == null)
			{
                entity = new ChatConversationReadStatus()
                {
                    UserId = userId,
                    LastReadChatMessageId = message.Id,
                    ChatConversationId = message.ChatConversationId,
                    LastReadAt = TimeHelper.GetCurrentUtcTime()
                };
                _unitOfWork.GetRepository<ChatConversationReadStatus>().Insert(entity);
			}
			else
			{
				entity.LastReadChatMessageId = message.Id;
				entity.LastReadAt = TimeHelper.GetCurrentUtcTime();
				_unitOfWork.GetRepository<ChatConversationReadStatus>().Update(entity);
			}
			await _unitOfWork.SaveAsync();
        }
    }
}
