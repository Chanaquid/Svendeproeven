using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(ReportFilter filter)
        {
            var query = _context.Reports.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            return await query.CountAsync();
        }

        public async Task<Report?> GetByIdAsync(int id)
        {
            return await _context.Reports.FindAsync(id);
        }

        public async Task<Report?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Reports
                .Include(r => r.ReportedBy)
                .Include(r => r.HandledByAdmin)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PagedResult<Report>> GetAllAsync(ReportFilter? filter, PagedRequest request)
        {
            var query = _context.Reports
                .AsNoTracking()
                .Include(r => r.ReportedBy)
                .Include(r => r.HandledByAdmin)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Report>> GetByUserIdAsync(string userId, ReportFilter? filter, PagedRequest request)
        {
            var query = _context.Reports
                .AsNoTracking()
                .Include(r => r.ReportedBy) 
                .Include(r => r.HandledByAdmin)
                .Where(r => r.ReportedById == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<DateTime?> GetLastReportTimeByUserAsync(string userId)
        {
            return await _context.Reports
                .AsNoTracking()
                .Where(r => r.ReportedById == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => (DateTime?)r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasReportedTargetAsync(string userId, string targetId, ReportType type)
        {
            return await _context.Reports
                .AsNoTracking()
                .AnyAsync(r => r.ReportedById == userId
                    && r.TargetId == targetId
                    && r.Type == type);
        }

        public async Task AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
        }

        public void Update(Report report)
        {
            _context.Reports.Update(report);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Dynamic filtering and sorting
        private static IQueryable<Report> ApplyFilter(IQueryable<Report> query, ReportFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.ReportedById))
                query = query.Where(r => r.ReportedById == filter.ReportedById);

            if (!string.IsNullOrWhiteSpace(filter.HandledByAdminId))
                query = query.Where(r => r.HandledByAdminId == filter.HandledByAdminId);

            if (!string.IsNullOrWhiteSpace(filter.TargetId))
                query = query.Where(r => r.TargetId == filter.TargetId);

            if (filter.Type.HasValue)
                query = query.Where(r => r.Type == filter.Type.Value);

            if (filter.Reasons.HasValue)
                query = query.Where(r => r.Reasons == filter.Reasons.Value);

            if (filter.Status.HasValue)
                query = query.Where(r => r.Status == filter.Status.Value);

            //IsResolved only applies when Status is not already filtered
            if (filter.IsResolved.HasValue && !filter.Status.HasValue)
            {
                query = filter.IsResolved.Value
                    ? query.Where(r => r.Status == ReportStatus.Resolved || r.Status == ReportStatus.Dismissed)
                    : query.Where(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview);
            }

            if (filter.CreatedAfter.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.CreatedBefore.Value);

            if (filter.ResolvedAfter.HasValue)
                query = query.Where(r => r.ResolvedAt.HasValue && r.ResolvedAt.Value >= filter.ResolvedAfter.Value);

            if (filter.ResolvedBefore.HasValue)
                query = query.Where(r => r.ResolvedAt.HasValue && r.ResolvedAt.Value <= filter.ResolvedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(r =>
                    (r.AdditionalDetails != null && r.AdditionalDetails.ToLower().Contains(search)) ||
                    (r.AdminNote != null && r.AdminNote.ToLower().Contains(search)) ||
                    r.Type.ToString().ToLower().Contains(search) ||   
                    r.Reasons.ToString().ToLower().Contains(search));  
            }

            return query;
        }

        private static IQueryable<Report> ApplySorting(IQueryable<Report> query, PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(r => r.CreatedAt);
        }
    }
}