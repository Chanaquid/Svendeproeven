using backend.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        private CallerContext? _callerContext;

        //Current authenticated user (cached per request)
        protected CallerContext Caller
        {
            get
            {
                if (_callerContext == null)
                {
                    _callerContext = BuildCallerContext();
                }

                return _callerContext;
            }
        }

        //Builds CallerContext from JWT/Claims
        private CallerContext BuildCallerContext()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAppException("User not authenticated");

            return new CallerContext
            {
                UserId = userId,
                IsAdmin = User.IsInRole("Admin"),
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                UserName = User.FindFirst("username")?.Value,
                AvatarUrl = User.FindFirst("avatarUrl")?.Value
            };
        }

        //eturns caller or null for optional-auth endpoints
        protected CallerContext? GetCallerOrNull()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return null;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return new CallerContext
            {
                UserId = userId,
                IsAdmin = User.IsInRole("Admin"),
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                UserName = User.FindFirst("username")?.Value,
                AvatarUrl = User.FindFirst("avatarUrl")?.Value
            };
        }

        //Ensures current user is admin
        protected void EnsureAdmin()
        {
            if (!Caller.IsAdmin)
                throw new ForbiddenException("Admin access required");
        }

        //Ensures current user owns the resource or is admin
        protected void EnsureOwnerOrAdmin(string ownerId)
        {
            if (!Caller.IsAdmin && Caller.UserId != ownerId)
                throw new ForbiddenException("You don't have permission to access this resource");
        }

        //Ensures current user matches specific userId -> admin denied too
        protected void EnsureUser(string userId)
        {
            if (Caller.UserId != userId)
                throw new ForbiddenException("You can only access your own resources");
        }
    }
}