using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class DirectConversationService : IDirectConversationService
    {
        private readonly IDirectConversationRepository _directConversationRepository;
        private readonly IDirectMessageRepository _directMessageRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DirectConversationService(
            IDirectConversationRepository directConversationRepository,
            IDirectMessageRepository directMessageRepository,
            UserManager<ApplicationUser> userManager)
        {
            _directConversationRepository = directConversationRepository;
            _directMessageRepository = directMessageRepository;
            _userManager = userManager;
        }

        public async Task<DirectConversationDto> GetOrCreateConversationAsync(
            string currentUserId,
            string otherUserId,
            string? initialMessage = null)
        {
            if (currentUserId == otherUserId)
                throw new ArgumentException("Cannot create a conversation with yourself.");

            var areBlocked = await _directConversationRepository.AreUsersBlockedAsync(currentUserId, otherUserId);
            if (areBlocked)
                throw new InvalidOperationException("This conversation is unavailable.");

            bool isNew = false;
            var conversation = await _directConversationRepository.GetConversationBetweenUsersAsync(currentUserId, otherUserId);

            if (conversation == null)
            {
                conversation = await _directConversationRepository.CreateAsync(currentUserId, otherUserId);
                isNew = true;
            }

            //Send initial message only if conversation is new and message provided
            if (isNew && !string.IsNullOrWhiteSpace(initialMessage))
            {
                await _directMessageRepository.AddAsync(new DirectMessage
                {
                    ConversationId = conversation.Id,
                    SenderId = currentUserId,
                    Content = initialMessage.Trim(),
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });

                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.MessageCount = 1;
                _directConversationRepository.Update(conversation);
                await _directConversationRepository.SaveChangesAsync();
            }

            return await GetConversationAsync(conversation.Id, currentUserId);
        }

        public async Task<DirectConversationDto> GetConversationAsync(int conversationId, string currentUserId)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != currentUserId && conversation.OtherUserId != currentUserId)
                throw new UnauthorizedAccessException("You don't have access to this conversation.");

            var isInitiator = conversation.InitiatedById == currentUserId;
            var otherUserId = isInitiator ? conversation.OtherUserId : conversation.InitiatedById;
            var otherUser = isInitiator ? conversation.OtherUser : conversation.InitiatedBy;
            var userDeletedAt = isInitiator ? conversation.InitiatorDeletedAt : conversation.OtherDeletedAt;

            var areBlocked = await _directConversationRepository.AreUsersBlockedAsync(currentUserId, otherUserId);

            //No unread count if blocked — blocked users don't generate notifications
            var unreadCount = areBlocked
                ? 0
                : await _directMessageRepository.GetUnreadCountForConversationAsync(conversationId, currentUserId, userDeletedAt);

            var messagesResult = await _directMessageRepository.GetConversationMessagesAsync(
                conversationId,
                currentUserId,
                null,
                new PagedRequest { Page = 1, PageSize = 50, SortBy = "SentAt", SortDescending = false });

            return new DirectConversationDto
            {
                Id = conversation.Id,
                InitiatedById = conversation.InitiatedById,
                InitiatedByFullName = conversation.InitiatedBy?.FullName ?? "Unknown",
                InitiatedByUserName = conversation.InitiatedBy?.UserName ?? "Unknown",
                InitiatedByAvatarUrl = conversation.InitiatedBy?.AvatarUrl,
                OtherUserId = otherUserId,
                OtherUserFullName = otherUser?.FullName ?? "Unknown",
                OtherUserName = otherUser?.UserName ?? "Unknown",
                OtherUserAvatarUrl = otherUser?.AvatarUrl,
                HiddenForInitiator = conversation.HiddenForInitiator,
                HiddenForOther = conversation.HiddenForOther,
                InitiatorDeletedAt = conversation.InitiatorDeletedAt,
                OtherDeletedAt = conversation.OtherDeletedAt,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                IsHiddenForCurrentUser = isInitiator ? conversation.HiddenForInitiator : conversation.HiddenForOther,
                CanSendMessage = !areBlocked,
                IsBlocked = areBlocked,
                UnreadCount = unreadCount,
                Messages = messagesResult.Items.Select(m => new DirectMessageDto
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
                    IsMine = m.SenderId == currentUserId
                }).ToList()
            };
        }

        public async Task<PagedResult<DirectConversationListDto>> GetUserConversationsAsync(
            string currentUserId,
            ConversationFilter? filter,
            PagedRequest request)
        {
            var conversations = await _directConversationRepository.GetUserConversationsAsync(currentUserId, filter, request);

            //Batch fetch — single query each, no N+1
            var blockedUserIds = await _directConversationRepository.GetBlockedUserIdsAsync(currentUserId);
            var unreadCounts = await _directConversationRepository.GetUnreadCountsForUserAsync(currentUserId);

            var items = conversations.Items.Select(conversation =>
            {
                var isInitiator = conversation.InitiatedById == currentUserId;
                var otherUserId = isInitiator ? conversation.OtherUserId : conversation.InitiatedById;
                var otherUser = isInitiator ? conversation.OtherUser : conversation.InitiatedBy;
                var areBlocked = blockedUserIds.Contains(otherUserId);

                //Blocked conversations stay in inbox but show no unread count and no message preview
                var unreadCount = areBlocked ? 0 : unreadCounts.GetValueOrDefault(conversation.Id, 0);
                var lastMessageIsMe = conversation.LastMessage?.SenderId == currentUserId;

                return new DirectConversationListDto
                {
                    Id = conversation.Id,
                    OtherUserId = otherUserId,
                    OtherUserFullName = otherUser?.FullName ?? "Unknown",
                    OtherUserName = otherUser?.UserName ?? "Unknown",
                    OtherUserAvatarUrl = otherUser?.AvatarUrl,
                    LastMessageContent = areBlocked
                        ? "[Blocked User]"
                        : lastMessageIsMe
                            ? $"You: {conversation.LastMessage?.Content}"
                            : conversation.LastMessage?.Content,
                    LastMessageSentAt = conversation.LastMessage?.SentAt,
                    LastMessageSenderId = conversation.LastMessage?.SenderId,
                    LastMessageSenderName = areBlocked
                        ? null
                        : lastMessageIsMe
                            ? "You"
                            : otherUser?.UserName,
                    LastMessageAvatarUrl = areBlocked
                        ? null
                        : lastMessageIsMe
                            ? null //frontend uses caller's own avatar
                            : otherUser?.AvatarUrl,
                    UnreadCount = unreadCount,
                    CreatedAt = conversation.CreatedAt,
                    IsInitiatedByMe = isInitiator,
                    IsBlocked = areBlocked
                };
            }).ToList();

            return new PagedResult<DirectConversationListDto>
            {
                Items = items,
                TotalCount = conversations.TotalCount,
                Page = conversations.Page,
                PageSize = conversations.PageSize
            };
        }

        public async Task DeleteConversationForUserAsync(int conversationId, string currentUserId)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != currentUserId && conversation.OtherUserId != currentUserId)
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");

            var now = DateTime.UtcNow;

            if (conversation.InitiatedById == currentUserId)
            {
                conversation.HiddenForInitiator = true;
                //Timestamp acts as message visibility cursor — user only sees messages after this point
                conversation.InitiatorDeletedAt = now;
            }
            else
            {
                conversation.HiddenForOther = true;
                conversation.OtherDeletedAt = now;
            }

            _directConversationRepository.Update(conversation);
            await _directConversationRepository.SaveChangesAsync();
        }

        public async Task RestoreConversationForUserAsync(int conversationId, string currentUserId)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (conversation.InitiatedById != currentUserId && conversation.OtherUserId != currentUserId)
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");

            if (conversation.InitiatedById == currentUserId)
            {
                conversation.HiddenForInitiator = false;
                // Clear the visibility cursor so all messages are visible again
                conversation.InitiatorDeletedAt = null;
            }
            else
            {
                conversation.HiddenForOther = false;
                conversation.OtherDeletedAt = null;
            }

            _directConversationRepository.Update(conversation);
            await _directConversationRepository.SaveChangesAsync();
        }

        public async Task<bool> CanUserAccessConversationAsync(int conversationId, string userId)
        {
            var conversation = await _directConversationRepository.GetByIdAsync(conversationId);
            return conversation != null &&
                   (conversation.InitiatedById == userId || conversation.OtherUserId == userId);
        }

        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            return await _directMessageRepository.GetTotalUnreadCountForUserAsync(userId);
        }

        public async Task<UnreadCountsDto> GetUnreadCountsPerConversationAsync(string userId)
        {
            //Both single queries — no N+1
            var blockedUserIds = await _directConversationRepository.GetBlockedUserIdsAsync(userId);
            var unreadCounts = await _directConversationRepository.GetUnreadCountsForUserAsync(userId);

            var conversations = await _directConversationRepository.GetUserConversationsAsync(
                userId,
                new ConversationFilter { IncludeHidden = false },
                new PagedRequest { Page = 1, PageSize = 1000 });

            var result = new UnreadCountsDto();

            foreach (var conversation in conversations.Items)
            {
                var isInitiator = conversation.InitiatedById == userId;
                var otherUserId = isInitiator ? conversation.OtherUserId : conversation.InitiatedById;

                //Skip blocked conversations — no unread notifications
                if (blockedUserIds.Contains(otherUserId)) continue;

                var unreadCount = unreadCounts.GetValueOrDefault(conversation.Id, 0);
                if (unreadCount > 0)
                {
                    result.ConversationUnreadCounts[conversation.Id] = unreadCount;
                    result.TotalUnreadCount += unreadCount;
                }
            }

            return result;
        }
    }
}