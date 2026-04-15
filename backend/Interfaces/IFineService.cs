using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IFineService
    {
        //Admin — issue fines
        Task<FineDto> CreateLoanDisputeFineAsync(string adminId, CreateLoanDisputeFineDto dto);
        Task<FineDto> CreateCustomFineAsync(string adminId, CreateCustomFineDto dto);
        Task<FineDto> UpdateFineAsync(string adminId, UpdateFineDto dto);
        Task VoidFineAsync(string adminId, int fineId);

        //User — payment flow
        Task<FineDto> SubmitPaymentProofAsync(string userId, SubmitPaymentProofDto dto);
        Task<FineDto> VerifyPaymentAsync(string adminId,int fineId, AdminFineVerifyPaymentDto dto);

        //User query
        Task<FineDto> GetFineByIdAsync(int fineId, string userId);
        Task<PagedResult<FineListDto>> GetMyFinesAsync(string userId, FineFilter? filter, PagedRequest request);

        //Admin query
        Task<FineDto> AdminGetFineByIdAsync(int fineId);
        Task<PagedResult<FineListDto>> GetAllFinesAsync(FineFilter? filter, PagedRequest request);
        Task<PagedResult<FineListDto>> GetFinesByStatusAsync(FineStatus status, FineFilter? filter, PagedRequest request);
        Task<PagedResult<FineListDto>> GetPendingProofReviewAsync(FineFilter? filter, PagedRequest request);
        Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId);
        Task<List<FineDto>> GetFinesByDisputeIdAsync(int disputeId);
        Task<PagedResult<FineListDto>> GetAllFinesByUserIdAsync(string userId, FineFilter? filter, PagedRequest request, bool isAdmin = false);


        //Stats
        Task<FineStatsDto> GetFineStatsAsync();

        //Guards
        Task<bool> UserHasUnpaidFinesAsync(string userId);
    }
}