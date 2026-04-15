using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IAdminUserService
    {

        //User management
        Task<AdminUserDto> GetUserByIdAsync(string userId); //more details than userservice call
        Task<AdminUserDto> GetUserByIdWIthDetailsAsync(string userId); //Full detail view of user -> seperate Apis
        Task<AdminUserDto?> GetUserByIdIgnoreFiltersAsync(string userId); //includes deletedUsers


        Task<AdminUserDto> AdminEditUserAsync(string targetUserId, string adminId, AdminEditUserDto dto);

        Task<AdminDeleteResultDto> AdminSoftDeleteUserAsync(string targetUserId, string adminId, string? note = null);

        //Filtered lists
        Task<PagedResult<AdminUserDto>> GetUsersAsync(UserFilter? filter, PagedRequest request); //Get all users/can use filter
        Task<PagedResult<AdminUserDto>> GetAllBannedUsersAsync(UserFilter? filter, PagedRequest request, bool tempBansOnly = false);
        Task<PagedResult<AdminUserDto>> GetAllUsersIncludingDeletedAsync(UserFilter? filter, PagedRequest request); //Get all users uncluding deleted ones


        //Ban
        Task BanUserAsync(string targetUserId, string adminId, BanUserDto dto);
        Task UnbanUserAsync(string targetUserId, string adminId, UnbanUserDto dto);

    }
}