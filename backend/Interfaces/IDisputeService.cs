using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IDisputeService
    {
        Task<DisputeDto> CreateDisputeAsync(string userId, CreateDisputeDto dto);
        Task<DisputeDto> EditDisputeAsync(string userId, int disputeId, EditDisputeDto dto);
        Task CancelDisputeAsync(string userId, int disputeId);

        Task<DisputePhotoDto> AddFiledByPhotoUrlAsync(string userId, int disputeId, string photoUrl);
        Task DeleteFiledByPhotoAsync(string userId, int disputeId, int photoId);


        Task MarkViewedByOtherPartyAsync(string userId, int disputeId);
        Task<DisputeDto> SubmitResponseAsync(string userId, int disputeId, SubmitDisputeResponseDto dto);
        Task<DisputePhotoDto> AddResponsePhotoUrlAsync(string userId, int disputeId, string photoUrl);

        //Admin Resolution
        Task<DisputeDto> ResolveDisputeAsync(string adminId, int disputeId, AdminResolveDisputeDto dto);
         
        //Background task
        Task<int> ProcessExpiredDisputesAsync();

        //User
        Task<DisputeDto> GetDisputeByIdAsync(int disputeId, string userId);
        Task<PagedResult<DisputeListDto>> GetFiledByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetRespondedToByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetAllDisputesByUserIdAsync(string userId, DisputeFilter? filter, PagedRequest request);

        //Admin
        Task<DisputeDto> AdminGetDisputeByIdAsync(int disputeId);
        Task<PagedResult<DisputeListDto>> GetAllDisputesAsync(DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetAllOpenDisputesAsync(DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetDisputesByStatusAsync(DisputeStatus status, DisputeFilter? filter, PagedRequest request);
        Task<List<DisputeDto>> GetDisputesByLoanIdAsync(int loanId);
        Task<PagedResult<DisputeListDto>> GetDisputeHistoryByItemAsync(int itemId, DisputeFilter? filter, PagedRequest request);

        //Stats
        Task<DisputeStatsDto> GetDisputeStatsAsync();

        //Guards
        Task<bool> CanUserFileDisputeAsync(string userId, int loanId);
        Task<bool> IsUserPartyToDisputeAsync(string userId, int disputeId);
        Task<bool> IsEditableAsync(int disputeId);
    }
}