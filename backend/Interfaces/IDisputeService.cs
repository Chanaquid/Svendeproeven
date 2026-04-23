using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IDisputeService
    {
        //User actions
        Task<DisputeDto> CreateDisputeAsync(string userId, CreateDisputeDto dto);
        Task<DisputeDto> EditDisputeAsync(string userId, int disputeId, EditDisputeDto dto);
        Task CancelDisputeAsync(string userId, int disputeId);
        Task<DisputeDto> SubmitResponseAsync(string userId, int disputeId, SubmitDisputeResponseDto dto);
        Task MarkViewedByOtherPartyAsync(string userId, int disputeId);

        //Photos
        Task<DisputePhotoDto> AddFiledByPhotoUrlAsync(string userId, int disputeId, string photoUrl, string? caption);
        Task DeleteFiledByPhotoAsync(string userId, int disputeId, int photoId);
        Task<DisputePhotoDto> AddResponsePhotoUrlAsync(string userId, int disputeId, string photoUrl, string? caption);

        //User queries
        Task<DisputeDto> GetDisputeByIdAsync(int disputeId, string userId);
        Task<PagedResult<DisputeListDto>> GetFiledByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetRespondedToByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetAllDisputesByUserIdAsync(string userId, DisputeFilter? filter, PagedRequest request);

        //Admin actions
        Task<DisputeDto> ResolveDisputeAsync(string adminId, int disputeId, AdminResolveDisputeDto dto);

        //Admin queries
        Task<DisputeDto> AdminGetDisputeByIdAsync(int disputeId, string requestingUserId);
        Task<PagedResult<DisputeListDto>> GetAllDisputesAsync(string requestingUserId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetAllOpenDisputesAsync(string requestingUserId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetDisputesByStatusAsync(string requestingUserId, DisputeStatus status, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<DisputeListDto>> GetDisputeHistoryByItemAsync(string requestingUserId, int itemId, DisputeFilter? filter, PagedRequest request);
        Task<List<DisputeDto>> GetDisputesByLoanIdAsync(int loanId);
        Task<DisputeStatsDto> GetDisputeStatsAsync();

        //Utilities
        Task<bool> CanUserFileDisputeAsync(string userId, int loanId);
        Task<bool> IsUserPartyToDisputeAsync(string userId, int disputeId);
        Task<bool> IsEditableAsync(int disputeId);
        Task<int> ProcessExpiredDisputesAsync();
    }
}