using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    //JUST logs, ban/unban is in adminuser domain
    public class UserBanHistoryService : IUserBanHistoryService
    {
        private readonly IUserBanHistoryRepository _banHistoryRepository;
        private readonly IUserRepository _userRepository;

        public UserBanHistoryService(
            IUserBanHistoryRepository banHistoryRepository,
            IUserRepository userRepository)
        {
            _banHistoryRepository = banHistoryRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedResult<UserBanHistoryDto>> GetByUserIdAsync(
            string userId,
            UserBanHistoryFilter? filter,
            PagedRequest request)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var paged = await _banHistoryRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged);
        }

        public async Task<PagedResult<UserBanHistoryDto>> GetAllAsync(
            UserBanHistoryFilter? filter,
            PagedRequest request)
        {
            var paged = await _banHistoryRepository.GetAllAsync(filter, request);
            return MapPagedResult(paged);
        }

        public async Task<UserBanHistoryDto> GetByIdAsync(int id)
        {
            var entry = await _banHistoryRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Ban history entry {id} not found.");

            return MapToDto(entry);
        }

        //Helpers

        private static UserBanHistoryDto MapToDto(UserBanHistory b)
        {
            return new UserBanHistoryDto
            {
                Id = b.Id,
                BannedUserId = b.UserId,
                BannedFullName = b.User?.FullName ?? string.Empty,
                BannedUserName = b.User?.UserName ?? string.Empty,
                BannedUserAvatarUrl = b.User?.AvatarUrl,
                AdminId = b.AdminId,
                AdminFullName = b.Admin?.FullName ?? string.Empty,
                AdminUserName = b.Admin?.UserName ?? string.Empty,
                AdminAvatarUrl = b.Admin?.AvatarUrl,
                IsBanned = b.IsBanned,
                Reason = b.Reason,
                Note = b.Note,
                BannedAt = b.BannedAt,
                BanExpiresAt = b.BanExpiresAt
            };
        }

        private static PagedResult<UserBanHistoryDto> MapPagedResult(PagedResult<UserBanHistory> source)
        {
            return new PagedResult<UserBanHistoryDto>
            {
                Items = source.Items.Select(MapToDto).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}