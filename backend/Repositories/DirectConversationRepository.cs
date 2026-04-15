using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;


namespace backend.Repositories
{
    public class DirectConversationRepository : IDirectConversationRepository
    {
        private readonly ApplicationDbContext _context;

        public DirectConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DirectConversation?> GetByIdAsync(int conversationId)
        {
            return await _context.DirectConversations
                .Include(c => c.InitiatedBy)
                .Include(c => c.OtherUser)
                .Include(c => c.LastMessage)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }


        //Ensures consistent ordering to avoid duplicate conversations
        public async Task<DirectConversation?> GetConversationBetweenUsersAsync(string userId1, string userId2)
        {
            var initiatedById = string.Compare(userId1, userId2) < 0 ? userId1 : userId2;
            var otherUserId = initiatedById == userId1 ? userId2 : userId1;

            return await _context.DirectConversations
                .FirstOrDefaultAsync(c =>
                    c.InitiatedById == initiatedById &&
                    c.OtherUserId == otherUserId);
        }


        public async Task<DirectConversation> CreateAsync(string userId1, string userId2)
        {
            //Always store with smaller ID as initiator for deduplication
            var initiator = string.Compare(userId1, userId2) < 0 ? userId1 : userId2;
            var other = initiator == userId1 ? userId2 : userId1;

            var conversation = new DirectConversation
            {
                InitiatedById = initiator,
                OtherUserId = other,
                CreatedAt = DateTime.UtcNow
            };

            _context.DirectConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<PagedResult<DirectConversation>> GetUserConversationsAsync(
            string userId,
            ConversationFilter? filter,
            PagedRequest request)
        {
            var query = _context.DirectConversations
                .Include(c => c.InitiatedBy)
                .Include(c => c.OtherUser)
                .Include(c => c.LastMessage)
                    .ThenInclude(m => m.Sender)
                .Where(c => c.InitiatedById == userId || c.OtherUserId == userId)
                .AsQueryable();

            //respect hidden flag unless explicitly including hidden
            if (filter?.IncludeHidden != true)
            {
                query = query.Where(c =>
                    (c.InitiatedById == userId && !c.HiddenForInitiator) ||
                    (c.OtherUserId == userId && !c.HiddenForOther));
            }

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var search = filter.Search.Trim().ToLowerInvariant();

                    query = query.Where(c =>
                        (c.InitiatedById == userId &&
                            (
                                (c.OtherUser.UserName != null && c.OtherUser.UserName.ToLower().Contains(search)) ||
                                (c.OtherUser.FullName != null && c.OtherUser.FullName.ToLower().Contains(search))
                            ))
                        ||
                        (c.OtherUserId == userId &&
                            (
                                (c.InitiatedBy.UserName != null && c.InitiatedBy.UserName.ToLower().Contains(search)) ||
                                (c.InitiatedBy.FullName != null && c.InitiatedBy.FullName.ToLower().Contains(search))
                            )));

                    //Exclude blocked users during search
                    query = query.Where(c =>
                        !_context.UserBlocks.Any(b =>
                            (b.BlockerId == userId && b.BlockedId == (c.InitiatedById == userId ? c.OtherUserId : c.InitiatedById)) ||
                            (b.BlockerId == (c.InitiatedById == userId ? c.OtherUserId : c.InitiatedById) && b.BlockedId == userId)));
                }

                if (filter.LastMessageAfter.HasValue)
                    query = query.Where(c => c.LastMessageAt >= filter.LastMessageAfter.Value);

                if (filter.LastMessageBefore.HasValue)
                    query = query.Where(c => c.LastMessageAt <= filter.LastMessageBefore.Value);
            }

            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "LastMessageAt" : request.SortBy;
            query = query.ApplySorting(sortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }


        //Single query — batch fetch blocked user IDs for performance (no n+1)
        public async Task<HashSet<string>> GetBlockedUserIdsAsync(string userId)
        {
            var blocked = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
                .ToListAsync();

            return blocked.ToHashSet();
        }

        //Unread messages grouped by conversation
        public async Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(string userId)
        {
            return await _context.DirectMessages
                .Where(m =>
                    m.SenderId != userId &&
                    !m.IsRead &&
                    (
                        (m.Conversation.InitiatedById == userId && !m.Conversation.HiddenForInitiator) ||
                        (m.Conversation.OtherUserId == userId && !m.Conversation.HiddenForOther)
                    ))
                .GroupBy(m => m.ConversationId)
                .Select(g => new { ConversationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ConversationId, x => x.Count);
        }

        //Checks if either user has blocked the other
        public async Task<bool> AreUsersBlockedAsync(string userId1, string userId2)
        {
            return await _context.UserBlocks
                .AnyAsync(b =>
                    (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                    (b.BlockerId == userId2 && b.BlockedId == userId1));
        }

        public void Update(DirectConversation conversation)
        {
            _context.DirectConversations.Update(conversation);
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}




