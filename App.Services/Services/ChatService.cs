using App.Core.Base;
using App.Core.Constants;
using App.Core.Utils;
using App.DTOs.ChatDTOs;
using App.Repositories.Models.Chat;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
	public class ChatService : IChatService
	{
		private IUnitOfWork _unitOfWork;
		private IUserService _userService;

		// Cache cho conversations để giảm database queries
		private static readonly Dictionary<string, (DateTime Timestamp, ICollection<ChatConversationDTO> Data)> _conversationsCache = new();
		private static readonly object _cacheLock = new object();
		private const int CACHE_EXPIRY_SECONDS = 15;

		public ChatService(IUnitOfWork unitOfWork, IUserService userService)
		{
			_unitOfWork = unitOfWork;
			_userService = userService;
		}

		/// <summary>
		/// Gets all chat conversations that a specific UserId joined (not ChatParticipantId)
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task<ICollection<ChatConversationDTO>> GetConversationsByUserIdAsync(int page, int size)
		{
			var userId = _userService.GetCurrentUserId();
			
			// Kiểm tra cache trước
			string cacheKey = $"conversations_{userId}_{page}_{size}";
			lock (_cacheLock)
			{
				if (_conversationsCache.TryGetValue(cacheKey, out var cachedData))
				{
					if ((DateTime.UtcNow - cachedData.Timestamp).TotalSeconds < CACHE_EXPIRY_SECONDS)
					{
						return cachedData.Data;
					}
					_conversationsCache.Remove(cacheKey);
				}
			}
			
			// Tối ưu query để giảm memory usage
			var conversations = await _unitOfWork.GetRepository<ChatConversation>()
				.ExistEntities()
				.Where(e => e.AppUsers.Any(x => x.Id.Equals(userId)))
				.OrderByDescending(c => c.CreatedTime)
				.Skip((page - 1) * size)
				.Take(size)
				.Select(c => new ChatConversationDTO
				{
					Id = c.Id,
					Messages = c.ChatMessages
						.OrderByDescending(m => m.CreatedTime)
						.Take(5)
						.Select(m => m.ToChatMessageDTO())
						.ToList(),
					Participants = c.AppUsers
						.Select(u => u.ToChatParticipantDTO())
						.ToList(),
					ChatConversationReadStatus = c.ChatConversationReadStatus
						.Select(r => r.ToDTO())
						.ToList()
				})
				.ToListAsync();
				
			// Lưu vào cache
			lock (_cacheLock)
			{
				_conversationsCache[cacheKey] = (DateTime.UtcNow, conversations);
				
				// Dọn cache nếu quá lớn
				if (_conversationsCache.Count > 50)
				{
					var oldestKey = _conversationsCache
						.OrderBy(x => x.Value.Timestamp)
						.First().Key;
					_conversationsCache.Remove(oldestKey);
				}
			}
				
			return conversations;
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
			var conversation = await _unitOfWork.GetRepository<ChatConversation>()
				.ExistEntities()
				.Include(c => c.ChatMessages.OrderByDescending(c => c.CreatedTime).Skip((page - 1) * size).Take(20))
				.Include(c => c.AppUsers)
				.Include(c => c.ChatConversationReadStatus)
				.FirstOrDefaultAsync(e => e.Id.Equals(id));
			
			if (conversation == null)
				throw new ErrorException(404, ErrorCode.NotFound, "CONVERSATION_NOT_FOUND");
			
			return conversation.ToChatConversationDTO();
		}

		/// <summary>
		/// Creates a new message. If there's no conversation corresponding to the user
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		/// <exception cref="ErrorException"></exception>
		public async Task<ChatMessageDTO> SendMessageAsync(SendMessageRequest request)
		{
			if (request.SenderUserId.Equals(request.ReceiverUserId))
				throw new ErrorException((int)StatusCode.BadRequest, ErrorCode.BadRequest, "SENDER_ID_SAME_AS_RECEIVER_ID");

			// Giới hạn kích thước tin nhắn
			if (request.TextMessage?.Length > 2000)
			{
				request.TextMessage = request.TextMessage.Substring(0, 2000);
			}

			// Tối ưu query để giảm memory usage
			var conversation = await _unitOfWork.GetRepository<ChatConversation>()
				.ExistEntities()
				.Where(e => e.AppUsers.Any(x => x.Id.Equals(request.SenderUserId)) && 
							e.AppUsers.Any(x => x.Id.Equals(request.ReceiverUserId)))
				.FirstOrDefaultAsync();
				
			if(conversation == null)
				conversation = await CreateConversation(new CreateConversationRequest()
				{
					ParticipantUserIds = new[] { request.SenderUserId, request.ReceiverUserId },
				});

			var message = new ChatMessage()
			{
				AppUserId = request.SenderUserId,
				TextMessage = request.TextMessage,
				ChatConversationId = conversation!.Id,
			};
			_unitOfWork.GetRepository<ChatMessage>().Insert(message);
			await _unitOfWork.SaveAsync();
			
			// Xóa cache liên quan
			ClearConversationCache(request.SenderUserId);
			ClearConversationCache(request.ReceiverUserId);

			return message.ToChatMessageDTO();
		}

		public async Task<ChatConversation?> CreateConversation(CreateConversationRequest request)
		{
			var conversation = new ChatConversation()
			{
				AppUsers = new List<AppUser>(),
				ChatMessages = new List<ChatMessage>()
			};

			if (request.ParticipantUserIds == null)
				throw new ErrorException(400, ErrorCode.BadRequest, "PARTICIPANT_USER_IDS_REQUIRED");

			var users = await _unitOfWork.GetRepository<AppUser>()
				.ExistEntities()
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
			// Giới hạn kích thước tin nhắn
			if (request.TextMessage?.Length > 2000)
			{
				request.TextMessage = request.TextMessage.Substring(0, 2000);
			}

			var message = await _unitOfWork.GetRepository<ChatMessage>().ExistEntities()
				.FirstOrDefaultAsync(x => x.Id.Equals(request.Id) && x.DeletedTime == null);
				
			if (message == null)
				throw new ErrorException((int)StatusCode.NotFound, ErrorCode.NotFound, "CHAT_MESSAGE_NOT_FOUND");

			message.TextMessage = request.TextMessage;
			_unitOfWork.GetRepository<ChatMessage>().Update(message);
			await _unitOfWork.SaveAsync();
			
			// Xóa cache liên quan
			var conversation = await _unitOfWork.GetRepository<ChatConversation>()
				.ExistEntities()
				.Where(c => c.Id == message.ChatConversationId)
				.Include(c => c.AppUsers)
				.FirstOrDefaultAsync();
				
			if (conversation != null)
			{
				foreach (var user in conversation.AppUsers)
				{
					ClearConversationCache(user.Id);
				}
			}
			
			return message.ToChatMessageDTO();
		}

		public async Task DeleteMessageAsync(string id)
		{
			var entity = await _unitOfWork.GetRepository<ChatMessage>().ExistEntities()
				.Include(m => m.ChatConversation)
				.ThenInclude(c => c.AppUsers)
				.FirstOrDefaultAsync(e => e.Id.Equals(id) && e.DeletedTime == null);
				
			if (entity != null)
			{
				// Lấy danh sách user ID trước khi xóa
				var userIds = entity.ChatConversation.AppUsers.Select(u => u.Id).ToList();
				
				_unitOfWork.GetRepository<ChatMessage>().Delete(entity);
				await _unitOfWork.SaveAsync();
				
				// Xóa cache liên quan
				foreach (var userId in userIds)
				{
					ClearConversationCache(userId);
				}
			}
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
			
			// Xóa cache liên quan
			ClearConversationCache(userId);
		}
		
		// Helper method để xóa cache liên quan đến user
		private void ClearConversationCache(string userId)
		{
			lock (_cacheLock)
			{
				var keysToRemove = _conversationsCache.Keys
					.Where(k => k.StartsWith($"conversations_{userId}_"))
					.ToList();
					
				foreach (var key in keysToRemove)
				{
					_conversationsCache.Remove(key);
				}
			}
		}
	}
}
