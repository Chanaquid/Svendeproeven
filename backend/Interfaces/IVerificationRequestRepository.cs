using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IVerificationRequestRepository
    {
        Task<int> CountAsync(VerificationRequestFilter filter);
        Task<VerificationRequest?> GetByIdAsync(int id);
        Task<VerificationRequest?> GetByIdWithDetailsAsync(int id);
        Task<VerificationRequest?> GetPendingByUserIdAsync(string userId);
        Task<PagedResult<VerificationRequest>> GetByUserIdAsync(string userId, VerificationRequestFilter? filter, PagedRequest request);
        Task<PagedResult<VerificationRequest>> GetAllAsync(VerificationRequestFilter? filter, PagedRequest request);
        Task<bool> HasPendingRequestAsync(string userId);
        Task AddAsync(VerificationRequest request);
        void Update(VerificationRequest request);
        Task SaveChangesAsync();
    }
}