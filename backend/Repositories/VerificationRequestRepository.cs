using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class VerificationRequestRepository : IVerificationRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public VerificationRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<int> CountAsync(VerificationRequestFilter filter)
        {
            var query = _context.VerificationRequests.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            return await query.CountAsync();
        }

        public async Task<VerificationRequest?> GetByIdAsync(int id)
        {
            return await _context.VerificationRequests.FindAsync(id);
        }

        public async Task<VerificationRequest?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.VerificationRequests
                .Include(v => v.User)
                .Include(v => v.ReviewedByAdmin)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<VerificationRequest?> GetPendingByUserIdAsync(string userId)
        {
            return await _context.VerificationRequests
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Status == VerificationStatus.Pending);
        }

        public async Task<PagedResult<VerificationRequest>> GetByUserIdAsync(
            string userId,
            VerificationRequestFilter? filter,
            PagedRequest request)
        {
            var query = _context.VerificationRequests
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.ReviewedByAdmin)
                .Where(v => v.UserId == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<VerificationRequest>> GetAllAsync(
            VerificationRequestFilter? filter,
            PagedRequest request)
        {
            var query = _context.VerificationRequests
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.ReviewedByAdmin)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<bool> HasPendingRequestAsync(string userId)
        {
            return await _context.VerificationRequests
                .AnyAsync(v => v.UserId == userId && v.Status == VerificationStatus.Pending);
        }

        public async Task AddAsync(VerificationRequest request)
        {
            await _context.VerificationRequests.AddAsync(request);
        }

        public void Update(VerificationRequest request)
        {
            _context.VerificationRequests.Update(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Helpers

        private static IQueryable<VerificationRequest> ApplyFilter(
            IQueryable<VerificationRequest> query,
            VerificationRequestFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(v => v.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.ReviewedByAdminId))
                query = query.Where(v => v.ReviewedByAdminId == filter.ReviewedByAdminId);

            if (filter.Status.HasValue)
                query = query.Where(v => v.Status == filter.Status.Value);

            if (filter.DocumentType.HasValue)
                query = query.Where(v => v.DocumentType == filter.DocumentType.Value);

          
            if (!filter.Status.HasValue)
            {
                if (filter.IsReviewed.HasValue)
                    query = filter.IsReviewed.Value
                        ? query.Where(v => v.Status != VerificationStatus.Pending)
                        : query.Where(v => v.Status == VerificationStatus.Pending);

                if (filter.IsApproved == true)
                    query = query.Where(v => v.Status == VerificationStatus.Approved);

                if (filter.IsRejected == true)
                    query = query.Where(v => v.Status == VerificationStatus.Rejected);
            }

            if (filter.SubmittedAfter.HasValue)
                query = query.Where(v => v.SubmittedAt >= filter.SubmittedAfter.Value);

            if (filter.SubmittedBefore.HasValue)
                query = query.Where(v => v.SubmittedAt <= filter.SubmittedBefore.Value);

            if (filter.ReviewedAfter.HasValue)
                query = query.Where(v => v.ReviewedAt.HasValue && v.ReviewedAt.Value >= filter.ReviewedAfter.Value);

            if (filter.ReviewedBefore.HasValue)
                query = query.Where(v => v.ReviewedAt.HasValue && v.ReviewedAt.Value <= filter.ReviewedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(v =>
                    v.DocumentType.ToString().ToLower().Contains(search) ||
                    (v.AdminNote != null && v.AdminNote.ToLower().Contains(search)) ||
                    (v.User != null && (
                        v.User.FullName.ToLower().Contains(search) ||
                        v.User.UserName!.ToLower().Contains(search)
                    ))
                );
            }

            return query;
        }

        private static IQueryable<VerificationRequest> ApplySorting(
            IQueryable<VerificationRequest> query,
            PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(v => v.SubmittedAt);
        }
    }
}