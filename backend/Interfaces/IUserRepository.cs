using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IUserRepository
    {
        //Basic user methods
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> GetByIdWithDetails(string userId);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByUsernameAsync(string username);

        //Checkers
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);

        //For landing
        Task<int> GetTotalUsersCountAsync();


        //CRUD
        void Add(ApplicationUser user);
        void Delete(ApplicationUser user);
        void Update(ApplicationUser user);
        Task SaveChangesAsync();

        //Get Users (for chat)
        Task<PagedResult<ApplicationUser>> SearchByUsernameOrEmailAsync(UserFilter? filter,
            PagedRequest request, string? currentUserId = null);

        //shortcut -> returns Dto but aight
        Task<UserPublicProfileDto?> GetPublicProfileByIdAsync(string userId, string currentUserId);

        //Helpers
        Task ExecuteInTransactionAsync(Func<Task> action);
    }
}