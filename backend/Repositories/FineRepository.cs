using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class FineRepository : IFineRepository
    {
        private readonly ApplicationDbContext _context;

        public FineRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(FineFilter filter)
        {
            var query = _context.Fines.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            return await query.CountAsync();
        }

        public async Task<decimal> SumAmountAsync(FineFilter filter)
        {
            var query = _context.Fines.AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.PaidAfter.HasValue)
                query = query.Where(x => x.PaidAt >= filter.PaidAfter.Value);

            return await query.SumAsync(x => x.Amount);
        }

        public async Task<Fine?> GetByIdAsync(int fineId)
        {
            return await _context.Fines
                .FirstOrDefaultAsync(f => f.Id == fineId);
        }

        public async Task<Fine?> GetByIdWithDetailsAsync(int fineId)
        {
            return await _context.Fines
                .AsSplitQuery()
                .Include(f => f.User)
                .Include(f => f.Loan)
                    .ThenInclude(l => l!.Item)
                .Include(f => f.Dispute)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.VerifiedByAdmin)
                .Include(f => f.Appeal)
                .FirstOrDefaultAsync(f => f.Id == fineId);
        }

        public async Task AddAsync(Fine fine)
        {
            await _context.Fines.AddAsync(fine);
        }

        public void Update(Fine fine)
        {
            _context.Fines.Update(fine);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //User
        public async Task<PagedResult<Fine>> GetByUserIdAsync(
            string userId, FineFilter? filter, PagedRequest request)
        {
            var query = _context.Fines
                .AsNoTracking()
                .Include(f => f.Loan)
                    .ThenInclude(l => l!.Item)
                .Include(f => f.User)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Dispute)
                .Include(f => f.Appeal)
                .Where(f => f.UserId == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(f => f.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        //Admin
        public async Task<PagedResult<Fine>> GetAllAsync(
            FineFilter? filter, PagedRequest request)
        {
            var query = _context.Fines
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Loan)
                    .ThenInclude(l => l!.Item)
                .Include(f => f.Dispute)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Appeal)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(f => f.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Fine>> GetByStatusAsync(
            FineStatus status, FineFilter? filter, PagedRequest request)
        {
            var query = _context.Fines
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Loan)
                    .ThenInclude(l => l!.Item)
                .Include(f => f.Dispute)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Appeal)
                .Where(f => f.Status == status)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(f => f.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Fine>> GetPendingProofReviewAsync(
            FineFilter? filter, PagedRequest request)
        {
            var query = _context.Fines
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Loan)
                    .ThenInclude(l => l!.Item)
                .Include(f => f.Dispute)
                .Include(f => f.Appeal)
                .Where(f => f.Status == FineStatus.PendingVerification)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            //Oldest proof first so nothing sits unreviewed
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderBy(f => f.ProofSubmittedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<List<Fine>> GetByLoanIdAsync(int loanId)
        {
            return await _context.Fines
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Appeal)
                .Where(f => f.LoanId == loanId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Fine>> GetByDisputeIdAsync(int disputeId)
        {
            return await _context.Fines
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.IssuedByAdmin)
                .Include(f => f.Appeal)
                .Where(f => f.DisputeId == disputeId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        //Checks
        public async Task<bool> ExistsActiveFineAsync(string userId, int? loanId, int? disputeId)
        {
            return await _context.Fines.AnyAsync(f =>
                f.UserId == userId &&
                f.LoanId == loanId &&
                f.DisputeId == disputeId &&
                f.Status != FineStatus.Voided &&
                f.Status != FineStatus.Paid);
        }

        public async Task<bool> ExistsPaidFineAsync(string userId, int? loanId, int? disputeId)
        {
            return await _context.Fines.AnyAsync(f =>
                f.UserId == userId &&
                f.Status == FineStatus.Paid &&
                f.LoanId == loanId &&
                f.DisputeId == disputeId);
        }

        public async Task<bool> HasOutstandingFinesAsync(string userId)
        {
            return await _context.Fines.AnyAsync(f =>
                f.UserId == userId &&
                (f.Status == FineStatus.Unpaid ||
                 f.Status == FineStatus.PendingVerification));
        }

        //Prevent n+1
        public async Task<Dictionary<string, decimal>> GetOutstandingTotalsByUsersAsync(List<string> userIds)
        {
            return await _context.Fines
                .Where(f => userIds.Contains(f.UserId) &&
                            (f.Status == FineStatus.Unpaid ||
                             f.Status == FineStatus.PendingVerification))
                .GroupBy(f => f.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(f => f.Amount) })
                .ToDictionaryAsync(x => x.UserId, x => x.Total);
        }

        //Totals (optimized queries)
        public async Task<decimal> GetOutstandingTotalByUserAsync(string userId)
        {
            return await _context.Fines
                .Where(f =>
                    f.UserId == userId &&
                    (f.Status == FineStatus.Unpaid ||
                     f.Status == FineStatus.PendingVerification))
                .SumAsync(f => f.Amount);
        }

        public async Task<decimal> GetOutstandingTotalAsync()
        {
            return await _context.Fines
                .Where(f =>
                    f.Status == FineStatus.Unpaid ||
                    f.Status == FineStatus.PendingVerification)
                .SumAsync(f => f.Amount);
        }

        public async Task<int> GetPendingProofCountAsync()
        {
            return await _context.Fines
                .CountAsync(f => f.Status == FineStatus.PendingVerification);
        }

        //Stats
        public async Task<Dictionary<FineStatus, int>> GetStatusCountsAsync()
        {
            return await _context.Fines
                .AsNoTracking()
                .GroupBy(f => f.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<FineType, int>> GetTypeCountsAsync()
        {
            return await _context.Fines
                .AsNoTracking()
                .GroupBy(f => f.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<int> GetIssuedThisMonthCountAsync()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await _context.Fines
                .AsNoTracking()
                .CountAsync(f => f.CreatedAt >= startOfMonth);
        }

        //Filtering
        private static IQueryable<Fine> ApplyFilter(IQueryable<Fine> query, FineFilter? filter)
        {
            if (filter is null) return query;

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(f => f.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.IssuedByAdminId))
                query = query.Where(f => f.IssuedByAdminId == filter.IssuedByAdminId);

            if (filter.Status.HasValue)
                query = query.Where(f => f.Status == filter.Status.Value);

            if (filter.Type.HasValue)
                query = query.Where(f => f.Type == filter.Type.Value);

            if (filter.LoanId.HasValue)
                query = query.Where(f => f.LoanId == filter.LoanId.Value);

            if (filter.DisputeId.HasValue)
                query = query.Where(f => f.DisputeId == filter.DisputeId.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(f => f.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(f => f.Amount <= filter.MaxAmount.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(f => f.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.PaidAfter.HasValue)
                query = query.Where(f =>
                    f.PaidAt.HasValue && f.PaidAt.Value >= filter.PaidAfter.Value);

            if (filter.HasPaymentProof.HasValue)
                query = filter.HasPaymentProof.Value
                    ? query.Where(f => f.PaymentProofImageUrl != null)
                    : query.Where(f => f.PaymentProofImageUrl == null);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(f =>
                    (f.User != null && f.User.FullName.ToLower().Contains(term)) ||
                    (f.User != null && f.User.UserName != null && f.User.UserName.ToLower().Contains(term)) ||
                    (f.AdminNote != null && f.AdminNote.ToLower().Contains(term)) ||
                    (f.PaymentDescription != null && f.PaymentDescription.ToLower().Contains(term)) ||
                    (f.Loan != null && f.Loan.Item != null && f.Loan.Item.Title.ToLower().Contains(term)));
            }

            return query;
        }
    }
}