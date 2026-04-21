using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Tests;

// Builds a ControllerContext with the right claims so BaseController.Caller works.
// Every controller in this project inherits BaseController which reads:
//   ClaimTypes.NameIdentifier -> UserId   (required, else throws UnauthorizedAppException)
//   "Admin" role              -> IsAdmin
//   ClaimTypes.Email / Name
//   "username" / "avatarUrl"  (custom claims)
// Without this, any call to Caller.UserId inside an action would throw.
internal static class ControllerTestHelper
{
    public static void SetUser(
        ControllerBase controller,
        string userId,
        bool isAdmin = false,
        string? email = "user@example.com",
        string? fullName = "Test User",
        string? username = "testuser",
        string? avatarUrl = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
        };

        if (!string.IsNullOrEmpty(email)) claims.Add(new Claim(ClaimTypes.Email, email));
        if (!string.IsNullOrEmpty(fullName)) claims.Add(new Claim(ClaimTypes.Name, fullName));
        if (!string.IsNullOrEmpty(username)) claims.Add(new Claim("username", username));
        if (!string.IsNullOrEmpty(avatarUrl)) claims.Add(new Claim("avatarUrl", avatarUrl));
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // Use this for endpoints that call GetCallerOrNull() — no identity at all.
    public static void SetAnonymous(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // not authenticated
            }
        };
    }
}