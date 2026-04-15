using backend.Dtos;
using backend.Helpers;
using backend.Models;
using backend.ProjectionModels;

namespace backend.Interfaces
{
    public interface IAdminUserRepository
    {
        //Get users
        Task<ApplicationUser?> GetUserByIdAsync(string userId); //Minimal info
        Task<UserWithCounts?> GetUserByIdWithDetailsAsync(string userId); //User with counts. --> for full view, we use seperate endpoints
        Task<ApplicationUser?> GetUserByIdIgnoreFiltersAsync(string userId); //Includes deletedUsers
        Task<PagedResult<UserWithRole>> GetAllBannedUsersAsync(UserFilter? filter, PagedRequest request, bool tempBansOnly = true);
        Task<List<ApplicationUser>> GetAllBannedUsersListAsync();
        Task<List<ApplicationUser>> GetExpiredBannedUsersAsync();

        //General listing with filters
        Task<PagedResult<UserWithRole>> GetUsersAsync(UserFilter? filter, PagedRequest request); //All filters -> not including deleted
        Task<PagedResult<UserWithRole>> GetAllUsersIncludingDeletedAsync(UserFilter? filter, PagedRequest request); //Get all users uncluding deleted ones

        void Update(ApplicationUser user);
        Task SaveChangesAsync();

        //Transaction
        Task ExecuteInTransactionAsync(Func<Task> action);

    }
}