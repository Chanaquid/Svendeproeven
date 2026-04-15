using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class DirectMessageRepository : IDirectMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public DirectMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DirectMessage message)
        {
            await _context.DirectMessages.AddAsync(message);
        }


        public async Task<PagedResult<DirectMessage>> GetConversationMessagesAsync(
            int conversationId,
            string userId,
            MessageFilter? filter,
            PagedRequest request)
        {
            var conversation = await _context.DirectConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return new PagedResult<DirectMessage>();

            var isInitiator = conversation.InitiatedById == userId;

            //User cannot see messages if they hid the conversation
            if (isInitiator && conversation.HiddenForInitiator)
                return new PagedResult<DirectMessage>();

            if (!isInitiator && conversation.HiddenForOther)
                return new PagedResult<DirectMessage>();

            //User is not a participant at all
            if (!isInitiator && conversation.OtherUserId != userId)
                return new PagedResult<DirectMessage>();

            var query = _context.DirectMessages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                .AsQueryable();

            //Hide messages sent before user deleted the conversation
            var userDeletedAt = isInitiator ? conversation.InitiatorDeletedAt : conversation.OtherDeletedAt;

            if (userDeletedAt.HasValue)
            {
                query = query.Where(m => m.SentAt > userDeletedAt.Value);
            }

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var search = filter.Search.Trim().ToLowerInvariant();
                    query = query.Where(m => m.Content.ToLower().Contains(search));
                }

                if (filter.SentAfter.HasValue)
                {
                    query = query.Where(m => m.SentAt >= filter.SentAfter.Value);
                }

                if (filter.SentBefore.HasValue)
                {
                    query = query.Where(m => m.SentAt <= filter.SentBefore.Value);
                }

                if (filter.IsRead.HasValue)
                {
                    query = query.Where(m => m.IsRead == filter.IsRead.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.SenderId))
                {
                    query = query.Where(m => m.SenderId == filter.SenderId);
                }
            }

            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "SentAt" : request.SortBy;
            query = query.ApplySorting(sortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }


        public async Task<int> GetUnreadCountForConversationAsync(int conversationId, string userId, DateTime? afterDate = null)
        {
            var conversation = await _context.DirectConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return 0;

            var isInitiator = conversation.InitiatedById == userId;

            if (isInitiator && conversation.HiddenForInitiator)
                return 0;

            if (!isInitiator && conversation.OtherUserId != userId)
                return 0;

            var query = _context.DirectMessages
                .Where(m => m.ConversationId == conversationId &&
                       m.SenderId != userId &&
                       !m.IsRead);

            if (afterDate.HasValue)
            {
                query = query.Where(m => m.SentAt > afterDate.Value);
            }

            return await query.CountAsync();
        }

        //Total unread messages across all conversations (respects per-user deletion)
        public async Task<int> GetTotalUnreadCountForUserAsync(string userId)
        {
            var query = _context.DirectMessages
                .Where(m => m.SenderId != userId && !m.IsRead)
                .Join(_context.DirectConversations,
                    m => m.ConversationId,
                    c => c.Id,
                    (m, c) => new { Message = m, Conversation = c })
                .Where(x => x.Conversation.InitiatedById == userId || x.Conversation.OtherUserId == userId)
                .Where(x => x.Message.SentAt > (
                    x.Conversation.InitiatedById == userId
                        ? (x.Conversation.InitiatorDeletedAt ?? DateTime.MinValue)
                        : (x.Conversation.OtherDeletedAt ?? DateTime.MinValue)
                ));

            return await query.CountAsync();
        }

        //Latest message for preview/sorting
        public async Task<DirectMessage?> GetLastMessageAsync(int conversationId)
        {
            return await _context.DirectMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task<DirectMessage?> GetByIdAsync(int messageId)
        {
            return await _context.DirectMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public void Update(DirectMessage message)
        {
            _context.DirectMessages.Update(message);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Bulk update for marking messages as read
        public async Task MarkMessagesAsReadAsync(int conversationId, string userId, int? upToMessageId = null)
        {
            var query = _context.DirectMessages
                .Where(m => m.ConversationId == conversationId &&
                       m.SenderId != userId &&
                       !m.IsRead);

            if (upToMessageId.HasValue)
            {
                query = query.Where(m => m.Id <= upToMessageId.Value);
            }

            await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, DateTime.UtcNow));
        }

        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            return await _context.DirectMessages
                .Where(m => m.ConversationId == conversationId)
                .CountAsync();
        }


    }
}

