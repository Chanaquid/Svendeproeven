using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class LoanMessageRepository : ILoanMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public LoanMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoanMessage?> GetByIdAsync(int id)
        {
            return await _context.LoanMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        //Get paged messages for a loan chat
        public async Task<PagedResult<LoanMessage>> GetByLoanIdAsync(int loanId, PagedRequest request)
        {
            var query = _context.LoanMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.LoanId == loanId)
                .AsQueryable();

            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "SentAt" : request.SortBy;
            query = query.ApplySorting(sortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        //Get latest message in a loan conversation
        public async Task<LoanMessage?> GetLastMessageAsync(int loanId)
        {
            return await _context.LoanMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.LoanId == loanId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetUnreadCountAsync(int loanId, string userId)
        {
            return await _context.LoanMessages
                .CountAsync(m =>
                    m.LoanId == loanId &&
                    m.SenderId != userId &&
                    !m.IsRead);
        }

        public async Task AddAsync(LoanMessage message)
        {
            await _context.LoanMessages.AddAsync(message);
        }

        public async Task<List<LoanMessage>> GetUnreadMessagesForUserAsync(int loanId, string userId)
        {
            return await _context.LoanMessages
                .Where(m => m.LoanId == loanId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();
        }

        // Mark messages as read up to optional message ID
        public async Task MarkAsReadAsync(int loanId, string userId, int? upToMessageId = null)
        {
            var query = _context.LoanMessages
                .Where(m =>
                    m.LoanId == loanId &&
                    m.SenderId != userId &&
                    !m.IsRead);

            if (upToMessageId.HasValue)
                query = query.Where(m => m.Id <= upToMessageId.Value);

            await query.ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsRead, true));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}