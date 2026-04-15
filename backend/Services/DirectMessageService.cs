using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class DirectMessageService : IDirectMessageService
    {
        private readonly IDirectMessageRepository _directMessageRepository;
        private readonly IDirectConversationRepository _directConversationRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DirectMessageService(
            IDirectMessageRepository directMessageRepository,
            IDirectConversationRepository directConversationRepository,
            UserManager<ApplicationUser> userManager)
        {
            _directMessageRepository = directMessageRepository;
            _directConversationRepository = directConversationRepository;
            _userManager = userManager;
        }

        public async Task<DirectMessageDto> SendMessageAsync(int conversationId, string senderId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message cannot be empty.");

            if (content.Length > 2000)
                throw new ArgumentException("Message cannot exceed 2000 characters.");

            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != senderId && conversation.OtherUserId != senderId)
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");

            var otherUserId = conversation.InitiatedById == senderId
                ? conversation.OtherUserId
                : conversation.InitiatedById;

            //Block check — direct messages are blocked between blocked users.
            if (await _directConversationRepository.AreUsersBlockedAsync(senderId, otherUserId))
                throw new InvalidOperationException("This conversation is unavailable.");

            var now = DateTime.UtcNow;

            var message = new DirectMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content.Trim(),
                SentAt = now,
                IsRead = false
            };

            await _directMessageRepository.AddAsync(message);

            //Update conversation metadata
            conversation.LastMessageAt = now;
            conversation.MessageCount += 1;

            //Reopen conversation for recipient if they had previously hidden it.
            if (conversation.InitiatedById == otherUserId && conversation.HiddenForInitiator)
            {
                conversation.HiddenForInitiator = false;
            }
            else if (conversation.OtherUserId == otherUserId && conversation.HiddenForOther)
            {
                conversation.HiddenForOther = false;
            }

            _directConversationRepository.Update(conversation);
            await _directConversationRepository.SaveChangesAsync();

            var sender = await _userManager.FindByIdAsync(senderId);

            return new DirectMessageDto
            {
                Id = message.Id,
                ConversationId = conversationId,
                SenderId = senderId,
                SenderFullName = sender?.FullName ?? "Unknown",
                SenderUserName = sender?.UserName ?? "Unknown",
                SenderAvatarUrl = sender?.AvatarUrl,
                Content = message.Content,
                SentAt = now,
                IsRead = false,
                IsMine = true
            };
        }

        public async Task<PagedResult<DirectMessageDto>> GetConversationMessagesAsync(
            int conversationId,
            string userId,
            MessageFilter? filter,
            PagedRequest request)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != userId && conversation.OtherUserId != userId)
                throw new UnauthorizedAccessException("You don't have access to this conversation.");

            var messages = await _directMessageRepository.GetConversationMessagesAsync(
                conversationId, userId, filter, request);

            var items = messages.Items.Select(m => new DirectMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderFullName = m.Sender?.FullName ?? "Unknown",
                SenderUserName = m.Sender?.UserName ?? "Unknown",
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Content = m.Content,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId
            }).ToList();

            return new PagedResult<DirectMessageDto>
            {
                Items = items,
                TotalCount = messages.TotalCount,
                Page = messages.Page,
                PageSize = messages.PageSize
            };
        }

        public async Task MarkMessagesAsReadAsync(int conversationId, string userId)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != userId && conversation.OtherUserId != userId)
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");

            await _directMessageRepository.MarkMessagesAsReadAsync(conversationId, userId);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _directMessageRepository.GetTotalUnreadCountForUserAsync(userId);
        }

        public async Task<DirectMessageDto?> GetLastMessageAsync(int conversationId, string currentUserId)
        {
            var message = await _directMessageRepository.GetLastMessageAsync(conversationId);
            if (message == null) return null;

            return new DirectMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderFullName = message.Sender?.FullName ?? "Unknown",
                SenderUserName = message.Sender?.UserName ?? "Unknown",
                SenderAvatarUrl = message.Sender?.AvatarUrl,
                Content = message.Content,
                IsRead = message.IsRead,
                SentAt = message.SentAt,
                IsMine = message.SenderId == currentUserId
            };
        }
    }
}