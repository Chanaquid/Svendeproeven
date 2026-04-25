using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services
{
    public class DirectMessageService : IDirectMessageService
    {
        private readonly IDirectMessageRepository _directMessageRepository;
        private readonly IDirectConversationRepository _directConversationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DirectChatHub> _chatHub;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly INotificationService _notificationService;
        private readonly IOnlineTracker _onlineTracker;

        public DirectMessageService(
            IDirectMessageRepository directMessageRepository,
            IDirectConversationRepository directConversationRepository,
            UserManager<ApplicationUser> userManager,
            IHubContext<DirectChatHub> chatHub,
            IHubContext<NotificationHub> notificationHub,
            INotificationService notificationService,
            IOnlineTracker onlineTracker)
        {
            _directMessageRepository = directMessageRepository;
            _directConversationRepository = directConversationRepository;
            _userManager = userManager;
            _chatHub = chatHub;
            _notificationHub = notificationHub;
            _notificationService = notificationService;
            _onlineTracker = onlineTracker;
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

            // Block check
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
            await _directMessageRepository.SaveChangesAsync();

            // Update conversation metadata
            conversation.LastMessageAt = now;
            conversation.MessageCount += 1;
            conversation.LastMessageId = message.Id;

            // Reopen for recipient if they had hidden the conversation
            if (conversation.InitiatedById == otherUserId && conversation.HiddenForInitiator)
                conversation.HiddenForInitiator = false;
            else if (conversation.OtherUserId == otherUserId && conversation.HiddenForOther)
                conversation.HiddenForOther = false;

            _directConversationRepository.Update(conversation);
            await _directConversationRepository.SaveChangesAsync();

            var sender = await _userManager.FindByIdAsync(senderId);

            var messageDto = new DirectMessageDto
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

            // Broadcast message to both parties in the conversation group (real-time chat)
            await _chatHub.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", messageDto);

            //Push sidebar update to recipient's personal group so their
            //conversation list reorders in real time even if they're on another page
            await _chatHub.Clients
                .Group($"user_{otherUserId}")
                .SendAsync("ConversationUpdated", new
                {
                    ConversationId = conversationId,
                    LastMessageContent = messageDto.Content,
                    LastMessageSentAt = messageDto.SentAt,
                    SenderId = messageDto.SenderId,
                    SenderFullName = messageDto.SenderFullName,
                    SenderAvatarUrl = messageDto.SenderAvatarUrl,
                });

            // If recipient is NOT currently viewing this chat, send notification
            var recipientOnline = _onlineTracker.IsUserInDirectChat(otherUserId, conversationId);

            if (recipientOnline)
            {
                // They're viewing the chat — mark as read immediately
                message.IsRead = true;
                await _directMessageRepository.SaveChangesAsync();
            }
            else
            {
                //Persist a notification so it shows in their notification list
                await _notificationService.SendAsync(
                    otherUserId,
                    NotificationType.DirectMessageReceived,
                    $"New message from {sender?.FullName ?? "Someone"}.",
                    conversationId,
                    NotificationReferenceType.DirectConversation);

                //Real-time bump — red dot on navbar bell even if they're on another page
                await _notificationHub.Clients
                    .Group($"user_{otherUserId}")
                    .SendAsync("NewMessageNotification", new
                    {
                        ConversationId = conversationId,
                        From = sender?.FullName ?? "Someone"
                    });
            }

            return messageDto;
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