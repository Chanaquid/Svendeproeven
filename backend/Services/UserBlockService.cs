using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly IUserBlockRepository _blockRepository;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserBlockService(
            IUserBlockRepository blockRepository,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager)
        {
            _blockRepository = blockRepository;
            _userRepository = userRepository;
            _userManager = userManager;
        }

        public async Task BlockUserAsync(string blockerId, string blockedId)
        {
            if (blockerId == blockedId)
                throw new InvalidOperationException("You cannot block yourself.");

            var blocker = await _userRepository.GetByIdAsync(blockerId)
                ?? throw new KeyNotFoundException("Blocker not found.");

            var blocked = await _userRepository.GetByIdAsync(blockedId)
                ?? throw new KeyNotFoundException("User to block not found.");

            //Admins cannot block other admins, users cannot block admins
            var blockedRoles = await _userManager.GetRolesAsync(blocked);
            if (blockedRoles.Contains(Roles.Admin))
                throw new InvalidOperationException("Admins cannot be blocked.");

            //Admins cannot block regular users either (for now)
            //var blockerRoles = await _userManager.GetRolesAsync(blocker);
            //if (blockerRoles.Contains(Roles.Admin))
            //    throw new InvalidOperationException("Admins cannot block users.");

            if (await _blockRepository.IsBlockedAsync(blockerId, blockedId))
                throw new InvalidOperationException("You have already blocked this user.");

            await _blockRepository.AddAsync(new UserBlock
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                CreatedAt = DateTime.UtcNow
            });

            await _blockRepository.SaveChangesAsync();
        }

        public async Task UnblockUserAsync(string blockerId, string blockedId)
        {
            var block = await _blockRepository.GetAsync(blockerId, blockedId)
                ?? throw new KeyNotFoundException("Block relationship not found.");

            await _blockRepository.DeleteAsync(block);
            await _blockRepository.SaveChangesAsync();
        }

        public async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
        {
            return await _blockRepository.IsBlockedAsync(blockerId, blockedId);
        }

        public async Task<bool> AreBlockedEitherWayAsync(string userId1, string userId2)
        {
            return await _blockRepository.AreBlockedEitherWayAsync(userId1, userId2);
        }

        public async Task<PagedResult<UserBlockListDto>> GetMyBlocksAsync(
            string userId,
            UserBlockFilter? filter,
            PagedRequest request)
        {
            var paged = await _blockRepository.GetBlocksByBlockerAsync(userId, filter, request);

            return new PagedResult<UserBlockListDto>
            {
                Items = paged.Items.Select(b => new UserBlockListDto
                {
                    BlockedId = b.BlockedId,
                    BlockedName = b.Blocked?.FullName ?? string.Empty,
                    BlockedUserName = b.Blocked?.UserName ?? string.Empty,
                    BlockedAvatarUrl = b.Blocked?.AvatarUrl,
                    CreatedAt = b.CreatedAt
                }).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task<PagedResult<UserBlockDto>> GetAllAsync(
            UserBlockFilter? filter,
            PagedRequest request)
        {
            var paged = await _blockRepository.GetAllAsync(filter, request);

            return new PagedResult<UserBlockDto>
            {
                Items = paged.Items.Select(MapToDto).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task AdminUnblockAsync(string blockerId, string blockedId)
        {
            var block = await _blockRepository.GetAsync(blockerId, blockedId)
                ?? throw new KeyNotFoundException("Block relationship not found.");

            await _blockRepository.DeleteAsync(block);
            await _blockRepository.SaveChangesAsync();
        }

        //Helpers

        private static UserBlockDto MapToDto(UserBlock b)
        {
            return new UserBlockDto
            {
                BlockerId = b.BlockerId,
                BlockerName = b.Blocker?.FullName ?? string.Empty,
                BlockerUserName = b.Blocker?.UserName ?? string.Empty,
                BlockerAvatarUrl = b.Blocker?.AvatarUrl,
                BlockedId = b.BlockedId,
                BlockedName = b.Blocked?.FullName ?? string.Empty,
                BlockedUserName = b.Blocked?.UserName ?? string.Empty,
                BlockedAvatarUrl = b.Blocked?.AvatarUrl,
                CreatedAt = b.CreatedAt
            };
        }
    }
}