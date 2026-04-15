using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IAppealService
    {
        //User actions
        Task<AppealDto> CreateScoreAppealAsync(string userId, CreateScoreAppealDto dto);
        Task<AppealDto> CreateFineAppealAsync(string userId, CreateFineAppealDto dto);
        Task<AppealDto> GetByIdAsync(int appealId, string userId, bool isAdmin = false);
        Task<PagedResult<AppealDto>> GetMyAppealsAsync(string userId, AppealFilter? filter, PagedRequest request);

        Task CancelAppealAsync(int appealId, string userId);
        Task DeleteAppealAsync(int appealId, string userId);

        //Admin actions
        Task<PagedResult<AppealDto>> GetAllAppealsByUserIdAsync(string userId, AppealFilter? filter, PagedRequest request);
        Task<AppealDto> GetByIdWithDetailsAsync(int appealId);
        Task<PagedResult<AppealDto>> GetAllAppealsAsync(AppealFilter? filter, PagedRequest request);
        Task<PagedResult<AppealDto>> GetAllPendingAsync(AppealFilter? filter, PagedRequest request);
        Task<PagedResult<AppealDto>> GetAllByStatusAsync(AppealStatus status, AppealFilter? filter, PagedRequest request);
        Task<AppealDto> DecideScoreAppealAsync(int appealId, string adminId, AdminDecidesScoreAppealDto dto);
        Task<AppealDto> DecideFineAppealAsync(int appealId, string adminId, AdminDecidesFineAppealDto dto);

    }
}