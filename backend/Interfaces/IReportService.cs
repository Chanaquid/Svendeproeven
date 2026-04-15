using backend.Dtos;

namespace backend.Interfaces
{
    public interface IReportService
    {
        //User
        Task<ReportDto> CreateReportAsync(string userId, CreateReportDto dto);
        Task<PagedResult<ReportDto>> GetMyReportsAsync(string userId, ReportFilter? filter, PagedRequest request);
        Task<ReportDto> GetByIdAsync(int id, string userId, bool isAdmin);

        //Admin
        Task<PagedResult<ReportDto>> GetAllAsync(ReportFilter? filter, PagedRequest request);
        Task<ReportDto> ResolveReportAsync(int id, string adminId, AdminResolveReportDto dto);
    }
}