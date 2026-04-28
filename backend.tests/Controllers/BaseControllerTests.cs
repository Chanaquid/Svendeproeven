using System.Security.Claims;
using backend.Common;
using backend.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace backend.Tests.Controllers
{
    public class BaseControllerTests
    {
        private class TestController : BaseController
        {
            public CallerContext GetCallerForTest()
            {
                return Caller;
            }

            public CallerContext? GetCallerOrNullForTest()
            {
                return GetCallerOrNull();
            }

            public void EnsureAdminForTest()
            {
                EnsureAdmin();
            }

            public void EnsureOwnerOrAdminForTest(string ownerId)
            {
                EnsureOwnerOrAdmin(ownerId);
            }

            public void EnsureUserForTest(string userId)
            {
                EnsureUser(userId);
            }
        }

        private TestController CreateController(
            string? userId = "user-123",
            bool isAuthenticated = true,
            bool isAdmin = false)
        {
            var controller = new TestController();

            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
            claims.Add(new Claim(ClaimTypes.Name, "Test User"));
            claims.Add(new Claim("username", "testuser"));
            claims.Add(new Claim("avatarUrl", "avatar.png"));

            var identity = isAuthenticated
                ? new ClaimsIdentity(claims, "TestAuth")
                : new ClaimsIdentity();

            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return controller;
        }

        [Fact]
        public void Caller_ShouldReturnCallerContext_WhenUserIsAuthenticated()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            var caller = controller.GetCallerForTest();

            // Assert
            caller.UserId.Should().Be("user-123");
            caller.IsAdmin.Should().BeFalse();
            caller.Email.Should().Be("test@example.com");
            caller.FullName.Should().Be("Test User");
            caller.UserName.Should().Be("testuser");
            caller.AvatarUrl.Should().Be("avatar.png");
        }

        [Fact]
        public void Caller_ShouldReturnIsAdminTrue_WhenUserHasAdminRole()
        {
            // Arrange
            var controller = CreateController(
                userId: "admin-123",
                isAuthenticated: true,
                isAdmin: true
            );

            // Act
            var caller = controller.GetCallerForTest();

            // Assert
            caller.UserId.Should().Be("admin-123");
            caller.IsAdmin.Should().BeTrue();
        }

        [Fact]
        public void Caller_ShouldThrowUnauthorizedAppException_WhenUserIdClaimIsMissing()
        {
            // Arrange
            var controller = CreateController(
                userId: null,
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.GetCallerForTest();

            // Assert
            act.Should()
                .Throw<UnauthorizedAppException>()
                .WithMessage("User not authenticated");
        }

        [Fact]
        public void GetCallerOrNull_ShouldReturnNull_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: false,
                isAdmin: false
            );

            // Act
            var caller = controller.GetCallerOrNullForTest();

            // Assert
            caller.Should().BeNull();
        }

        [Fact]
        public void GetCallerOrNull_ShouldReturnNull_WhenUserIdClaimIsMissing()
        {
            // Arrange
            var controller = CreateController(
                userId: null,
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            var caller = controller.GetCallerOrNullForTest();

            // Assert
            caller.Should().BeNull();
        }

        [Fact]
        public void GetCallerOrNull_ShouldReturnCallerContext_WhenUserIsAuthenticated()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            var caller = controller.GetCallerOrNullForTest();

            // Assert
            caller.Should().NotBeNull();
            caller!.UserId.Should().Be("user-123");
            caller.IsAdmin.Should().BeFalse();
            caller.Email.Should().Be("test@example.com");
        }

        [Fact]
        public void EnsureAdmin_ShouldNotThrow_WhenUserIsAdmin()
        {
            // Arrange
            var controller = CreateController(
                userId: "admin-123",
                isAuthenticated: true,
                isAdmin: true
            );

            // Act
            Action act = () => controller.EnsureAdminForTest();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureAdmin_ShouldThrowForbiddenException_WhenUserIsNotAdmin()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.EnsureAdminForTest();

            // Assert
            act.Should()
                .Throw<ForbiddenException>()
                .WithMessage("Admin access required");
        }

        [Fact]
        public void EnsureOwnerOrAdmin_ShouldNotThrow_WhenUserOwnsResource()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.EnsureOwnerOrAdminForTest("user-123");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureOwnerOrAdmin_ShouldNotThrow_WhenUserIsAdmin()
        {
            // Arrange
            var controller = CreateController(
                userId: "admin-123",
                isAuthenticated: true,
                isAdmin: true
            );

            // Act
            Action act = () => controller.EnsureOwnerOrAdminForTest("another-user");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureOwnerOrAdmin_ShouldThrowForbiddenException_WhenUserIsNotOwnerOrAdmin()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.EnsureOwnerOrAdminForTest("another-user");

            // Assert
            act.Should()
                .Throw<ForbiddenException>()
                .WithMessage("You don't have permission to access this resource");
        }

        [Fact]
        public void EnsureUser_ShouldNotThrow_WhenUserIdMatchesCaller()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.EnsureUserForTest("user-123");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureUser_ShouldThrowForbiddenException_WhenUserIdDoesNotMatchCaller()
        {
            // Arrange
            var controller = CreateController(
                userId: "user-123",
                isAuthenticated: true,
                isAdmin: false
            );

            // Act
            Action act = () => controller.EnsureUserForTest("another-user");

            // Assert
            act.Should()
                .Throw<ForbiddenException>()
                .WithMessage("You can only access your own resources");
        }

        [Fact]
        public void EnsureUser_ShouldThrowForbiddenException_EvenWhenAdminDoesNotMatch()
        {
            // Arrange
            var controller = CreateController(
                userId: "admin-123",
                isAuthenticated: true,
                isAdmin: true
            );

            // Act
            Action act = () => controller.EnsureUserForTest("user-123");

            // Assert
            act.Should()
                .Throw<ForbiddenException>()
                .WithMessage("You can only access your own resources");
        }
    }
}