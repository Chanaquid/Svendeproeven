using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<ApplicationUser?> GetByIdWithDetails(string userId)
        {
            return await _context.Users
                .AsSplitQuery() 
                .Include(u => u.OwnedItems)
                    .ThenInclude(i => i.Loans)
                .Include(u => u.ReviewsReceived)
                .Include(u => u.BorrowedLoans)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ApplicationUser?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }


        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }


        public void Add(ApplicationUser user)
        {
            _context.Users.Add(user);
        }

        public void Update(ApplicationUser user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
        }

        public void Delete(ApplicationUser user)
        {
            _context.Users.Remove(user);
        }
        

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<ApplicationUser>> SearchByUsernameOrEmailAsync(UserFilter? filter, PagedRequest request, string? currentUserId = null)
        {
            filter ??= new UserFilter();
            var query = _context.Users.AsQueryable();

            // Exclude blocked users
            if (!string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(u => !_context.UserBlocks.Any(b =>
                    (b.BlockerId == currentUserId && b.BlockedId == u.Id) ||
                    (b.BlockerId == u.Id && b.BlockedId == currentUserId)));
            }

            //exclude admins (for user-to-user DM search)
            if (filter.ExcludeAdmins == true)
            {
                var adminIds = await _context.UserRoles
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => new { ur.UserId, r.Name })
                    .Where(x => x.Name == "Admin")
                    .Select(x => x.UserId)
                    .ToHashSetAsync();

                query = query.Where(u => !adminIds.Contains(u.Id));
            }

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                    query = query.Where(u => u.UserName.Contains(filter.Search) || u.Email.Contains(filter.Search));

                if (filter.IsBanned.HasValue)
                    query = query.Where(u => u.IsBanned == filter.IsBanned.Value);

                if (filter.IsDeleted.HasValue)
                    query = query.IgnoreQueryFilters().Where(u => u.IsDeleted == filter.IsDeleted.Value);

                if (filter.IsVerified.HasValue)
                    query = query.Where(u => u.IsVerified == filter.IsVerified.Value);

                if (filter.MinScore.HasValue)
                    query = query.Where(u => u.Score >= filter.MinScore.Value);

                if (filter.MaxScore.HasValue)
                    query = query.Where(u => u.Score <= filter.MaxScore.Value);

                if (filter.HasUnpaidFines == true)
                    query = query.Where(u => u.Fines.Any(f => f.Status == FineStatus.Unpaid));

                if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.RadiusKm.HasValue)
                {
                    double latitudeRange = filter.RadiusKm.Value / 111.0;
                    double longitudeRange = filter.RadiusKm.Value / (111.0 * Math.Cos(filter.Latitude.Value * (Math.PI / 180.0)));

                    query = query.Where(u =>
                        u.Latitude >= filter.Latitude.Value - latitudeRange &&
                        u.Latitude <= filter.Latitude.Value + latitudeRange &&
                        u.Longitude >= filter.Longitude.Value - longitudeRange &&
                        u.Longitude <= filter.Longitude.Value + longitudeRange);
                }
            }

            query = query.ApplySorting(request.SortBy, request.SortDescending);
            return await query.ToPagedResultAsync(request);
        }

        public async Task<UserPublicProfileDto?> GetPublicProfileByIdAsync(string userId, string currentUserId)
        {
            //Check if users are blocked - return null if blocked
            var areBlocked = await _context.UserBlocks
                .AnyAsync(b => (b.BlockerId == currentUserId && b.BlockedId == userId) ||
                               (b.BlockerId == userId && b.BlockedId == currentUserId));

            if (areBlocked)
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");


            //no need to load whole navs
            return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserPublicProfileDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.UserName,
                AvatarUrl = u.AvatarUrl,
                IsAdmin = isAdmin,
                Bio = u.Bio,
                Gender = u.Gender,
                GeneralAddress = u.Address,
                Age = DateTime.UtcNow.Year - u.DateOfBirth.Year -
                (DateTime.UtcNow.DayOfYear < u.DateOfBirth.DayOfYear ? 1 : 0),
                IsVerified = u.IsVerified,
                Score = u.Score,
                MembershipDate = u.MembershipDate,

                TotalItems = u.OwnedItems.Count(),
                TotalReviewsReceived = u.ReviewsReceived.Count(),
                TotalCompletedLoans = _context.Loans.Count(l =>
                (l.BorrowerId == u.Id || l.LenderId == u.Id) &&
                l.Status == LoanStatus.Completed)

            })
            .FirstOrDefaultAsync();
            }


        //For landing page 
        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users
                .CountAsync(u => !u.IsDeleted); //redundant?
        }


        //Wrap the db transactions in a transaction so if it fails midway, it rollbacks 
        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await action();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}