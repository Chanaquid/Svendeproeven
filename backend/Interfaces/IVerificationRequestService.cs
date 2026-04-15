using backend.Dtos;

namespace backend.Interfaces
{
    public interface IVerificationRequestService
    {
        Task<VerificationRequestDto> SubmitRequestAsync(string userId, CreateVerificationRequestDto dto);
        Task<PagedResult<VerificationRequestDto>> GetMyRequestsAsync(string userId, VerificationRequestFilter? filter, PagedRequest request);
        Task<VerificationRequestDto> GetByIdAsync(int id, string userId, bool isAdmin);

        //Admin
        Task<VerificationRequestDto> DecideAsync(int id, string adminId, AdminDecideVerificationRequestDto dto);
        Task<PagedResult<VerificationRequestDto>> GetAllAsync(VerificationRequestFilter? filter, PagedRequest request);
        Task<PagedResult<VerificationRequestDto>> GetByUserIdAsync(string userId, VerificationRequestFilter? filter, PagedRequest request);
    }
}