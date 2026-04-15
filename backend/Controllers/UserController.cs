using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        //Get own profile
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMyProfile()
        {
            var profile = await _userService.GetProfileAsync(Caller.UserId);
            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }


        //Get public profile (others)
        [HttpGet("{userId}/public")]
        public async Task<ActionResult<ApiResponse<UserPublicProfileDto>>> GetPublicProfile(string userId)
        {
            //Pass current user ID for block check - blocked users cannot see each other
            var profile = await _userService.GetPublicProfileAsync(userId, Caller.UserId);
            return Ok(ApiResponse<UserPublicProfileDto>.Ok(profile));
        }


        //Update own profile
        [HttpPut("me")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var updatedProfile = await _userService.UpdateProfileAsync(Caller.UserId, dto);
            return Ok(ApiResponse<UserProfileDto>.Ok(updatedProfile, "Profile updated successfully."));
        }

        [HttpPatch("me/avatar")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateAvatar([FromBody] UpdateAvatarDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserProfileDto>.Fail("Invalid avatar URL"));

            var updateDto = new UpdateProfileDto { AvatarUrl = dto.AvatarUrl };
            var updatedProfile = await _userService.UpdateProfileAsync(Caller.UserId, updateDto);
            return Ok(ApiResponse<UserProfileDto>.Ok(updatedProfile, "Avatar updated successfully."));
        }

        [HttpDelete("me")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            await _userService.DeleteAccountAsync(Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "Your account has been successfully deleted."));
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserProfileDto>>>> SearchUsers(
            [FromQuery] UserFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _userService.SearchUsersAsync(filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<UserProfileDto>>.Ok(result));
        }
    
    }
}