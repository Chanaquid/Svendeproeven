using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserService
    {
        //Profile
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<UserPublicProfileDto> GetPublicProfileAsync(string userId, string currentUserId);
        Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task DeleteAccountAsync(string userId, DeleteAccountDto dto);

        Task<PagedResult<UserProfileDto>> SearchUsersAsync(UserFilter? filter, PagedRequest request, string currentUserId);


        Task<int> GetTotalUsersCountAsync();

        //Guards — called by other services before allowing actions
        Task EnsureUserCanBorrowAsync(string userId);
        Task EnsureUserIsActiveAsync(string userId);



    }
}