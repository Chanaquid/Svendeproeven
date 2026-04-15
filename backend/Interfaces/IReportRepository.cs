using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IReportRepository
    {
        Task<Report?> GetByIdAsync(int id);
        Task<Report?> GetByIdWithDetailsAsync(int id);
        Task<PagedResult<Report>> GetAllAsync(ReportFilter? filter, PagedRequest request);
        Task<PagedResult<Report>> GetByUserIdAsync(string userId, ReportFilter? filter, PagedRequest request);
        Task<DateTime?> GetLastReportTimeByUserAsync(string userId);
        Task<bool> HasReportedTargetAsync(string userId, string targetId, ReportType type);
        Task AddAsync(Report report);
        void Update(Report report);
        Task SaveChangesAsync();
    }
}