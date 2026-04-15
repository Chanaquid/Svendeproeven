using backend.Common;
using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Helpers;
using backend.Interfaces;
using backend.Models;
using backend.ProjectionModels;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .FindAsync(userId);
        }

        //Includes deleted/banned users + includes related counts
        public async Task<UserWithCounts?> GetUserByIdWithDetailsAsync(string userId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => new UserWithCounts
                {
                    User = u,

                    BannedByAdminId = u.BannedByAdminId,
                    BannedByAdminName = u.BannedByAdmin != null ? u.BannedByAdmin.FullName : null,
                    BannedByAdminAvatarUrl = u.BannedByAdmin != null ? u.BannedByAdmin.AvatarUrl : null,
                    
                    DeletedByAdminId = u.DeletedByAdminId,
                    DeletedByAdminName = u.DeletedByAdmin != null ? u.DeletedByAdmin.FullName : null,
                    DeletedByAdminAvatarUrl = u.DeletedByAdmin != null ? u.DeletedByAdmin.AvatarUrl : null,
                    
                    //Counts
                    OwnedItemsCount = u.OwnedItems.Count(),
                    BorrowedLoansCount = u.BorrowedLoans.Count(),
                    GivenLoansCount = u.GivenLoans.Count(),
                    FinesCount = u.Fines.Count(),
                    ScoreHistoryCount = u.ScoreHistory.Count(),
                    BanHistoryCount = u.BanHistory.Count(),
                    VerificationRequestsCount = u.VerificationRequests.Count(),
                    AppealsCount = u.Appeals.Count(),
                    SupportThreadsCount = u.SupportThreads.Count(),
                    ItemReviewsCount = u.ItemReviews.Count(),
                    ReviewsGivenCount = u.ReviewsGiven.Count(),
                    ReviewsReceivedCount = u.ReviewsReceived.Count(),
                    InitiatedDisputesCount = u.InitiatedDisputes.Count(),
                    ReceivedDisputesCount = u.ReceivedDisputes.Count(),
                    ResolvedDisputesCount = u.ResolvedDisputes.Count(),
                    ResolvedAppealsCount = u.ResolvedAppeals.Count(),
                    ReviewedVerificationRequestsCount = u.ReviewedVerificationRequests.Count(),
                    ClaimedSupportThreadsCount = u.ClaimedSupportThreads.Count()
                })
                .FirstOrDefaultAsync();
                
        }


        //Bypasses global query filters (soft delete)
        public async Task<ApplicationUser?> GetUserByIdIgnoreFiltersAsync(string userId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<PagedResult<UserWithRole>> GetAllBannedUsersAsync(UserFilter? filter, PagedRequest request, bool tempBansOnly = false)
        {
            filter ??= new UserFilter();

            var baseQuery = _context.Users
                .Where(u => u.IsBanned)
                .AsQueryable();

            //Only temporary bans (with expiry)
            if (tempBansOnly)
                baseQuery = baseQuery.Where(u => u.BanExpiresAt != null);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var lowerSearch = filter.Search.ToLower();
                baseQuery = baseQuery.Where(u =>
                    u.FullName.ToLower().Contains(lowerSearch) ||
                    u.UserName!.ToLower().Contains(lowerSearch) ||
                    u.Email!.ToLower().Contains(lowerSearch));
            }
            if (filter.HasUnpaidFines == true)
                baseQuery = baseQuery.Where(u =>
                    u.Fines.Any(f => f.Status == FineStatus.Unpaid || f.Status == FineStatus.Rejected));

            //Join with roles (1 role per user)
            var query = from user in baseQuery
                        join userRole in _context.UserRoles on user.Id equals userRole.UserId into userRoles
                        from userRole in userRoles.DefaultIfEmpty()
                        join role in _context.Roles on userRole.RoleId equals role.Id into roles
                        from role in roles.DefaultIfEmpty()
                        select new UserWithRole
                        {
                            User = user,
                            Role = role != null ? role.Name! : Roles.User 
                        };

            //Dynamic Sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.FullName)
                    : query.OrderBy(x => x.User.FullName),

                "email" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Email)
                    : query.OrderBy(x => x.User.Email),

                "createdat" or null => request.SortDescending
                    ? query.OrderByDescending(x => x.User.CreatedAt)
                    : query.OrderBy(x => x.User.CreatedAt),

                "role" => request.SortDescending
                    ? query.OrderByDescending(x => x.Role)
                    : query.OrderBy(x => x.Role),

                "score" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Score)
                    : query.OrderBy(x => x.User.Score),

                "membershipdate" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.MembershipDate)
                    : query.OrderBy(x => x.User.MembershipDate),

                _ => query.OrderByDescending(x => x.User.CreatedAt)
            };

            return await query.ToPagedResultAsync(request);
        }

        public async Task<List<ApplicationUser>> GetAllBannedUsersListAsync()
        {
            return await _context.Users
                .Where(u => u.IsBanned)
                .ToListAsync();
        }

        //Used by background job to auto-unban users
        public async Task<List<ApplicationUser>> GetExpiredBannedUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsBanned &&
                            u.BanExpiresAt != null &&
                            u.BanExpiresAt <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<PagedResult<UserWithRole>> GetUsersAsync(UserFilter? filter, PagedRequest request)
        {
            filter ??= new UserFilter();


            var baseQuery = _context.Users
                .Where(u => !u.IsDeleted)
                .AsQueryable();


            //Filters
            if (filter.IsBanned.HasValue)
                baseQuery = baseQuery.Where(u => u.IsBanned == filter.IsBanned.Value);

            if (filter.IsVerified.HasValue)
                baseQuery = baseQuery.Where(u => u.IsVerified == filter.IsVerified.Value);

            if (filter.MinScore.HasValue)
                baseQuery = baseQuery.Where(u => u.Score >= (int)filter.MinScore.Value);

            if (filter.MaxScore.HasValue)
                baseQuery = baseQuery.Where(u => u.Score <= (int)filter.MaxScore.Value);


            if (filter.HasUnpaidFines == true)
                baseQuery = baseQuery.Where(u =>
                    u.Fines.Any(f => f.Status == FineStatus.Unpaid || f.Status == FineStatus.Rejected));

            //Rough geo filtering using bounding box
            if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.RadiusKm.HasValue)
            {
                double lat = filter.Latitude.Value;
                double lng = filter.Longitude.Value;
                double delta = filter.RadiusKm.Value / 111.0; //~1 degree = 111km

                baseQuery = baseQuery.Where(u =>
                    u.Latitude.HasValue &&
                    u.Longitude.HasValue &&
                    u.Latitude >= lat - delta &&
                    u.Latitude <= lat + delta &&
                    u.Longitude >= lng - delta &&
                    u.Longitude <= lng + delta);
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = filter.Role.Trim();

                baseQuery = baseQuery.Where(u =>
                    _context.UserRoles.Any(ur =>
                        ur.UserId == u.Id &&
                        _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)
                    ));
            }


            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = $"%{filter.Search.Trim()}%";

                baseQuery = baseQuery.Where(u =>
                    EF.Functions.Like(u.FullName, search) ||
                    EF.Functions.Like(u.UserName!, search) ||
                    EF.Functions.Like(u.Email!, search));
            }



            //Join roles (no grouping needed because 1 role per user)
            var query =
               from user in baseQuery
               join ur in _context.UserRoles on user.Id equals ur.UserId into userRoles
               from ur in userRoles.DefaultIfEmpty()
               join r in _context.Roles on ur.RoleId equals r.Id into roles
               from r in roles.DefaultIfEmpty()
               select new UserWithRole
               {
                   User = user,
                   Role = r != null ? r.Name! : Roles.User
               };

            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.FullName)
                    : query.OrderBy(x => x.User.FullName),

                "email" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Email)
                    : query.OrderBy(x => x.User.Email),

                "createdat" or null => request.SortDescending
                    ? query.OrderByDescending(x => x.User.CreatedAt)
                    : query.OrderBy(x => x.User.CreatedAt),

                "role" => request.SortDescending
                    ? query.OrderByDescending(x => x.Role)
                    : query.OrderBy(x => x.Role),

                "score" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Score)
                    : query.OrderBy(x => x.User.Score),

                "membershipdate" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.MembershipDate)
                    : query.OrderBy(x => x.User.MembershipDate),

                _ => query.OrderByDescending(x => x.User.CreatedAt)
            };

            //pagination
            return await query.ToPagedResultAsync(request);

        }

        public async Task<PagedResult<UserWithRole>> GetAllUsersIncludingDeletedAsync(UserFilter? filter, PagedRequest request)
        {
            filter ??= new UserFilter();

            var baseQuery = _context.Users.IgnoreQueryFilters().AsQueryable();


            //Filters
            if (filter.IsDeleted.HasValue)
                baseQuery = baseQuery.Where(u => u.IsDeleted == filter.IsDeleted.Value);


            if (filter.IsBanned.HasValue)
                baseQuery = baseQuery.Where(u => u.IsBanned == filter.IsBanned.Value);

            if (filter.IsVerified.HasValue)
                baseQuery = baseQuery.Where(u => u.IsVerified == filter.IsVerified.Value);

            if (filter.MinScore.HasValue)
                baseQuery = baseQuery.Where(u => u.Score >= (int)filter.MinScore.Value);

            if (filter.MaxScore.HasValue)
                baseQuery = baseQuery.Where(u => u.Score <= (int)filter.MaxScore.Value);

            if (filter.HasUnpaidFines == true)
                baseQuery = baseQuery.Where(u =>
                    u.Fines.Any(f => f.Status == FineStatus.Unpaid || f.Status == FineStatus.Rejected));


            if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.RadiusKm.HasValue)
            {
                double lat = filter.Latitude.Value;
                double lng = filter.Longitude.Value;
                double delta = filter.RadiusKm.Value / 111.0; //~1 degree = 111km

                baseQuery = baseQuery.Where(u =>
                    u.Latitude.HasValue &&
                    u.Longitude.HasValue &&
                    u.Latitude >= lat - delta &&
                    u.Latitude <= lat + delta &&
                    u.Longitude >= lng - delta &&
                    u.Longitude <= lng + delta);
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = filter.Role.Trim();

                baseQuery = baseQuery.Where(u =>
                    _context.UserRoles.Any(ur =>
                        ur.UserId == u.Id &&
                        _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)
                    ));
            }


            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = $"%{filter.Search.Trim()}%";

                baseQuery = baseQuery.Where(u =>
                    EF.Functions.Like(u.FullName, search) ||
                    EF.Functions.Like(u.UserName!, search) ||
                    EF.Functions.Like(u.Email!, search));
            }



            //Join roles (no grouping needed because 1 role per user)
            var query =
               from user in baseQuery
               join ur in _context.UserRoles on user.Id equals ur.UserId into userRoles
               from ur in userRoles.DefaultIfEmpty()
               join r in _context.Roles on ur.RoleId equals r.Id into roles
               from r in roles.DefaultIfEmpty()
               select new UserWithRole
               {
                   User = user,
                   Role = r != null ? r.Name! : Roles.User
               };

            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.FullName)
                    : query.OrderBy(x => x.User.FullName),

                "email" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Email)
                    : query.OrderBy(x => x.User.Email),

                "createdat" or null => request.SortDescending
                    ? query.OrderByDescending(x => x.User.CreatedAt)
                    : query.OrderBy(x => x.User.CreatedAt),

                "role" => request.SortDescending
                    ? query.OrderByDescending(x => x.Role)
                    : query.OrderBy(x => x.Role),

                "score" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.Score)
                    : query.OrderBy(x => x.User.Score),

                "membershipdate" => request.SortDescending
                    ? query.OrderByDescending(x => x.User.MembershipDate)
                    : query.OrderBy(x => x.User.MembershipDate),

                _ => query.OrderByDescending(x => x.User.CreatedAt)
            };

            //pagination
            return await query.ToPagedResultAsync(request);

        }


        public void Update(ApplicationUser user)
        {
            //Track last modification time
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

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