using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class DisputeRepository : IDisputeRepository
    {
        private readonly ApplicationDbContext _context;

        public DisputeRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<int> CountAsync(DisputeFilter filter)
        {
            var query = _context.Disputes.AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.ResolvedAfter.HasValue)
                query = query.Where(x => x.ResolvedAt >= filter.ResolvedAfter.Value);

            return await query.CountAsync();
        }

        public async Task<double> GetAverageResolutionDaysAsync()
        {
            return await _context.Disputes
                .Where(x => x.ResolvedAt != null)
                .AverageAsync(x => EF.Functions.DateDiffDay(x.CreatedAt, x.ResolvedAt!.Value));
        }

        public async Task<Dispute?> GetByIdAsync(int disputeId)
        {
            return await _context.Disputes
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        //Full graph load for dispute details view
        public async Task<Dispute?> GetByIdWithDetailsAsync(int disputeId)
        {
            return await _context.Disputes
                .AsSplitQuery()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.SnapshotPhotos)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.ResolvedByAdmin)
                .Include(d => d.Photos)
                .Include(d => d.Fines)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        public async Task AddAsync(Dispute dispute)
        {
            await _context.Disputes.AddAsync(dispute);
        }

        public void Update(Dispute dispute)
        {
            _context.Disputes.Update(dispute);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Dispute>> GetByLoanIdAsync(int loanId)
        {
            return await _context.Disputes
                .AsSplitQuery()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.SnapshotPhotos)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Include(d => d.Fines)
                .Where(d => d.LoanId == loanId)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();
        }

        //Active = still unresolved/requires action
        public async Task<Dispute?> GetActiveDisputeByLoanIdAsync(int loanId)
        {
            return await _context.Disputes
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d => d.LoanId == loanId && (
                    d.Status == DisputeStatus.AwaitingResponse ||
                    d.Status == DisputeStatus.PendingAdminReview ||
                    d.Status == DisputeStatus.PastDeadline))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasActiveDisputeAsync(int loanId)
        {
            return await _context.Disputes
                .AnyAsync(d => d.LoanId == loanId && (
                    d.Status == DisputeStatus.AwaitingResponse ||
                    d.Status == DisputeStatus.PendingAdminReview ||
                    d.Status == DisputeStatus.PastDeadline));
        }

        public async Task<bool> HasActiveDisputeByUserIdAsync(string userId)
        {
            return await _context.Disputes
                .AnyAsync(d =>
                    (d.FiledById == userId || d.RespondedById == userId) &&
                    (d.Status == DisputeStatus.AwaitingResponse ||
                     d.Status == DisputeStatus.PendingAdminReview ||
                     d.Status == DisputeStatus.PastDeadline));
        }

        public async Task<bool> HasUserFiledDisputeForLoanAsync(int loanId, string userId)
        {
            return await _context.Disputes
                .AnyAsync(d => d.LoanId == loanId && d.FiledById == userId);
        }

        public async Task<int> GetDisputeCountForLoanAsync(int loanId)
        {
            return await _context.Disputes
                .CountAsync(d => d.LoanId == loanId);
        }

        public async Task<PagedResult<Dispute>> GetFiledByUserAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes
                .AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d => d.FiledById == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetRespondedToByUserAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes
                .AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d =>
                    d.RespondedById == userId ||
                    (d.FiledById != userId &&
                     (d.Status == DisputeStatus.AwaitingResponse ||
                      d.Status == DisputeStatus.PastDeadline) &&
                     (d.Loan.Item.OwnerId == userId || d.Loan.BorrowerId == userId)))
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetAllDisputesByUserIdAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes
                .AsNoTracking()
                 .AsSplitQuery()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d => d.FiledById == userId
                 || d.RespondedById == userId
                 || d.Loan.LenderId == userId   
                 || d.Loan.BorrowerId == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetAllAsync(
            DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes.AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetAllOpenAsync(
            DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes.AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d =>
                    d.Status == DisputeStatus.AwaitingResponse ||
                    d.Status == DisputeStatus.PendingAdminReview ||
                    d.Status == DisputeStatus.PastDeadline)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetByStatusAsync(
            DisputeStatus status, DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes.AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d => d.Status == status)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Dispute>> GetDisputeHistoryByItemIdAsync(
            int itemId, DisputeFilter? filter, PagedRequest request)
        {
            var query = _context.Disputes.AsNoTracking()
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Lender)
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(d => d.FiledBy)
                .Include(d => d.RespondedBy)
                .Include(d => d.Photos)
                .Where(d => d.Loan.ItemId == itemId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(d => d.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }


        //Used by background job to detect overdue responses
        public async Task<List<Dispute>> GetExpiredAwaitingResponseAsync()
        {
            return await _context.Disputes
                .Include(d => d.FiledBy)
                .Where(d =>
                    d.Status == DisputeStatus.AwaitingResponse &&
                    d.ResponseDeadline < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<int> GetOpenCountAsync()
        {
            return await _context.Disputes.AsNoTracking()
                .CountAsync(d =>
                    d.Status == DisputeStatus.AwaitingResponse ||
                    d.Status == DisputeStatus.PendingAdminReview ||
                    d.Status == DisputeStatus.PastDeadline);
        }

        public async Task<int> GetPastDeadlineCountAsync()
        {
            return await _context.Disputes.AsNoTracking()
                .CountAsync(d => d.Status == DisputeStatus.PastDeadline);
        }

        public async Task<int> GetResolvedCountByMonthAsync(int year, int month)
        {
            return await _context.Disputes.AsNoTracking()
                .CountAsync(d =>
                    d.Status == DisputeStatus.Resolved &&
                    d.ResolvedAt.HasValue &&
                    d.ResolvedAt.Value.Year == year &&
                    d.ResolvedAt.Value.Month == month);
        }

        //Aggregated counts for dashboards
        public async Task<Dictionary<DisputeStatus, int>> GetStatusCountsAsync()
        {
            return await _context.Disputes.AsNoTracking()
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<DisputeVerdict, int>> GetVerdictCountsAsync()
        {
            return await _context.Disputes.AsNoTracking()
                .Where(d => d.AdminVerdict.HasValue)
                .GroupBy(d => d.AdminVerdict!.Value)
                .Select(g => new { Verdict = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Verdict, x => x.Count);
        }

        public async Task AddPhotoAsync(DisputePhoto photo)
        {
            await _context.DisputePhotos.AddAsync(photo);
        }

        public async Task<DisputePhoto?> GetPhotoByIdAsync(int photoId)
        {
            return await _context.DisputePhotos
                .FirstOrDefaultAsync(p => p.Id == photoId);
        }

        public async Task<List<DisputePhoto>> GetPhotosByDisputeIdAsync(int disputeId)
        {
            return await _context.DisputePhotos.AsNoTracking()
                .Where(p => p.DisputeId == disputeId)
                .OrderBy(p => p.UploadedAt)
                .ToListAsync();
        }

        public Task DeletePhotoAsync(DisputePhoto photo)
        {
            _context.DisputePhotos.Remove(photo);
            return Task.CompletedTask;
        }

        //Apply filters
        private static IQueryable<Dispute> ApplyFilter(
            IQueryable<Dispute> query, DisputeFilter? filter)
        {
            if (filter is null) return query;

            if (!string.IsNullOrWhiteSpace(filter.FiledById))
                query = query.Where(d => d.FiledById == filter.FiledById);

            if (!string.IsNullOrWhiteSpace(filter.RespondedById))
                query = query.Where(d => d.RespondedById == filter.RespondedById);

            if (!string.IsNullOrWhiteSpace(filter.ResolvedByAdminId))
                query = query.Where(d => d.ResolvedByAdminId == filter.ResolvedByAdminId);

            if (filter.LoanId.HasValue)
                query = query.Where(d => d.LoanId == filter.LoanId.Value);

            if (filter.FiledAs.HasValue)
                query = query.Where(d => d.FiledAs == filter.FiledAs.Value);

            if (filter.Status.HasValue)
                query = query.Where(d => d.Status == filter.Status.Value);

            if (filter.AdminVerdict.HasValue)
                query = query.Where(d => d.AdminVerdict == filter.AdminVerdict.Value);

            if (filter.IsResolved.HasValue)
                query = filter.IsResolved.Value
                    ? query.Where(d => d.Status == DisputeStatus.Resolved)
                    : query.Where(d => d.Status != DisputeStatus.Resolved);

            if (filter.HasResponse.HasValue)
                query = filter.HasResponse.Value
                    ? query.Where(d => d.RespondedById != null)
                    : query.Where(d => d.RespondedById == null);

            if (filter.IsOverdueResponse.HasValue && filter.IsOverdueResponse.Value)
                query = query.Where(d =>
                    d.Status == DisputeStatus.AwaitingResponse &&
                    d.ResponseDeadline < DateTime.UtcNow);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(d => d.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(d => d.CreatedAt <= filter.CreatedBefore.Value);

            if (filter.ResolvedAfter.HasValue)
                query = query.Where(d => d.ResolvedAt.HasValue &&
                    d.ResolvedAt.Value >= filter.ResolvedAfter.Value);

            if (filter.ResolvedBefore.HasValue)
                query = query.Where(d => d.ResolvedAt.HasValue &&
                    d.ResolvedAt.Value <= filter.ResolvedBefore.Value);

            if (filter.ResponseDeadlineBefore.HasValue)
                query = query.Where(d => d.ResponseDeadline <= filter.ResponseDeadlineBefore.Value);

            if (filter.ResponseDeadlineAfter.HasValue)
                query = query.Where(d => d.ResponseDeadline >= filter.ResponseDeadlineAfter.Value);

            if (filter.MinCustomFine.HasValue)
                query = query.Where(d => d.CustomFineAmount.HasValue &&
                    d.CustomFineAmount.Value >= filter.MinCustomFine.Value);

            if (filter.MaxCustomFine.HasValue)
                query = query.Where(d => d.CustomFineAmount.HasValue &&
                    d.CustomFineAmount.Value <= filter.MaxCustomFine.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(d =>
                    d.Description.ToLower().Contains(term) ||
                    (d.ResponseDescription != null && d.ResponseDescription.ToLower().Contains(term)) ||
                    d.Loan.Item.Title.ToLower().Contains(term));
            }

            return query;
        }
    }
}