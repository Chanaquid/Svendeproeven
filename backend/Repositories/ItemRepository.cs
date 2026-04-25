using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(ItemFilter filter)
        {
            var query = _context.Items.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            if (filter.CreatedAfter.HasValue)
                query = query.Where(x => x.CreatedAt >= filter.CreatedAfter.Value);
            return await query.CountAsync();
        }

        //single item lookups
        public async Task<Item?> GetByIdAsync(int itemId)
        {
            return await _context.Items
                .FindAsync(itemId);
        }

        public async Task<Item?> GetByIdIncludingDeletedAsync(int itemId)
        {
            return await _context.Items
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<Item?> GetByIdWithDetailsAsync(int itemId)
        {
            return await _context.Items
                .AsSplitQuery()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Reviews)
                    .ThenInclude(i => i.Reviewer)
                .Include(i => i.Loans)
                    .ThenInclude(l => l.Disputes)
                .Include(i => i.ReviewedByAdmin)
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<Item?> GetBySlugAsync(string slug)
        {
            return await _context.Items
                .AsSplitQuery()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Reviews)
                    .ThenInclude(i => i.Reviewer)
                .Include(i => i.Loans)
                    .ThenInclude(l => l.Disputes)
                .Include(i => i.ReviewedByAdmin)
                .FirstOrDefaultAsync(i => i.Slug == slug && !i.IsDeleted);
        }

        public async Task<Item?> GetBySlugWithDetailsAsync(string slug)
        {
            return await _context.Items
                .AsSplitQuery()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Reviews)
                .Include(i => i.Loans)
                .Include(i => i.ReviewedByAdmin)
                .FirstOrDefaultAsync(i => i.Slug == slug);
        }

        public async Task<Item?> GetByQrCodeAsync(string qrCode)
        {
            return await _context.Items
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.QrCode == qrCode);
        }

        //Paged queries

        //Public browse — only approved + active items
        public async Task<PagedResult<Item>> GetAllApprovedAsync(
            ItemFilter? filter,
            PagedRequest request,
            IEnumerable<string>? excludeOwnerIds = null)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Reviews)
                .Where(i => i.Status == ItemStatus.Approved
                         && i.IsActive
                         && i.Availability != ItemAvailability.Unavailable)
                .AsQueryable();

            // Exclude blocked owners BEFORE pagination so TotalCount is correct
            query = ApplyOwnerExclusion(query, excludeOwnerIds);

            query = ApplyFilter(query, filter, publicOnly: true);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Admin — sees everything
        public async Task<PagedResult<Item>> GetAllAsAdminAsync(
            ItemFilter? filter,
            PagedRequest request)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.ReviewedByAdmin)
                .AsQueryable();

            query = ApplyFilter(query, filter, publicOnly: false);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Owner sees all their own items regardless of status
        public async Task<PagedResult<Item>> GetByOwnerIdAsync(
            string ownerId,
            ItemFilter? filter,
            PagedRequest request)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.OwnerId == ownerId)
                .AsQueryable();

            query = ApplyFilter(query, filter, publicOnly: false);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Public view of someone else's items — only approved + active
        public async Task<PagedResult<Item>> GetPublicByOwnerAsync(
            string ownerId,
            ItemFilter? filter,
            PagedRequest request)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.OwnerId == ownerId
                         && i.Status == ItemStatus.Approved
                         && i.IsActive)
                .AsQueryable();

            query = ApplyFilter(query, filter, publicOnly: true);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Item>> GetByCategoryAsync(
            int categoryId,
            ItemFilter? filter,
            PagedRequest request,
            IEnumerable<string>? excludeOwnerIds = null)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.CategoryId == categoryId
                         && i.Status == ItemStatus.Approved
                         && i.IsActive)
                .AsQueryable();

            // Exclude blocked owners BEFORE pagination so TotalCount is correct
            query = ApplyOwnerExclusion(query, excludeOwnerIds);

            query = ApplyFilter(query, filter, publicOnly: true);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Item>> GetPendingApprovalsAsync(
            ItemFilter? filter,
            PagedRequest request)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.Status == ItemStatus.Pending)
                .AsQueryable();

            query = ApplyFilter(query, filter, publicOnly: false);

            // Oldest first so nothing rots in the queue
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderBy(i => i.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        //Approved + active + Available status — for loan request flow
        public async Task<PagedResult<Item>> GetAvailableItemsAsync(
            ItemFilter? filter,
            PagedRequest request,
            IEnumerable<string>? excludeOwnerIds = null)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.Status == ItemStatus.Approved
                         && i.IsActive
                         && i.Availability == ItemAvailability.Available)
                .AsQueryable();

            // Exclude blocked owners BEFORE pagination so TotalCount is correct
            query = ApplyOwnerExclusion(query, excludeOwnerIds);

            query = ApplyFilter(query, filter, publicOnly: true);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Radius-based search using Haversine approximation
        public async Task<PagedResult<Item>> GetNearbyItemsAsync(
            double lat,
            double lon,
            double radiusKm,
            ItemFilter? filter,
            PagedRequest request,
            IEnumerable<string>? excludeOwnerIds = null)
        {
            //Pull approved + active candidates first, then filter by distance
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.Status == ItemStatus.Approved && i.IsActive)
                .AsQueryable();

            // Exclude blocked owners BEFORE pulling candidates into memory
            query = ApplyOwnerExclusion(query, excludeOwnerIds);

            query = ApplyFilter(query, filter, publicOnly: true);

            var candidates = await query.ToListAsync();

            var nearby = candidates
                .Select(i => new
                {
                    Item = i,
                    Distance = Haversine(lat, lon, i.PickupLatitude, i.PickupLongitude)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .ToList();

            var totalCount = nearby.Count;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var items = nearby
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Item)
                .ToList();

            return new PagedResult<Item>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        //Items whose AvailableUntil has passed but are still active — for expiry job
        public async Task<PagedResult<Item>> GetActiveItemsExpiredBeforeAsync(
            DateTime date,
            ItemFilter? filter,
            PagedRequest request)
        {
            var query = _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Where(i => i.IsActive && i.AvailableUntil < date)
                .AsQueryable();

            query = ApplyFilter(query, filter, publicOnly: false);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Utility queries
        public async Task<List<Item>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Items
                .Where(i => i.OwnerId == ownerId && !i.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> QrCodeExistsAsync(string qrCode)
        {
            return await _context.Items.AnyAsync(i => i.QrCode == qrCode);
        }

        public async Task<bool> IsOwnerAsync(int itemId, string userId)
        {
            return await _context.Items.AnyAsync(i => i.Id == itemId && i.OwnerId == userId);
        }

        public async Task<int> GetPendingApprovalsCountAsync()
        {
            return await _context.Items.CountAsync(i => i.Status == ItemStatus.Pending);
        }

        //for landing page (frontend)
        public async Task<List<Item>> GetNewestListedAsync(int count = 4)
        {
            return await _context.Items
                .AsNoTracking()
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Reviews)
                .Where(i => i.Status == ItemStatus.Approved
                         && i.IsActive
                         && i.Availability == ItemAvailability.Available)
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetAvailableCountAsync()
        {
            return await _context.Items
                .CountAsync(i => i.Status == ItemStatus.Approved
                              && i.IsActive
                              && i.Availability == ItemAvailability.Available
                              && !i.IsDeleted);
        }

        //Item Photos
        public async Task AddPhotoAsync(ItemPhoto photo)
        {
            await _context.ItemPhotos.AddAsync(photo);
        }

        public async Task<ItemPhoto?> GetPhotoByIdAsync(int photoId)
        {
            return await _context.ItemPhotos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.Id == photoId);
        }

        public void DeletePhoto(ItemPhoto photo)
        {
            _context.ItemPhotos.Remove(photo);
        }

        //CRUD

        public async Task AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
        }

        public void Update(Item item)
        {
            _context.Items.Update(item);
        }

        //Soft delete
        public void Delete(Item item)
        {
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //helpers
        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _context.Items.AnyAsync(i => i.Slug == slug);
        }

        private static IQueryable<Item> ApplyOwnerExclusion(
            IQueryable<Item> query,
            IEnumerable<string>? excludeOwnerIds)
        {
            if (excludeOwnerIds is null) return query;

            var excluded = excludeOwnerIds.ToHashSet();
            if (excluded.Count == 0) return query;

            return query.Where(i => !excluded.Contains(i.OwnerId));
        }

        private static IQueryable<Item> ApplyDefaultSort(IQueryable<Item> query, PagedRequest request)
        {
            return string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(i => i.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);
        }

        private static IQueryable<Item> ApplyFilter(
            IQueryable<Item> query,
            ItemFilter? filter,
            bool publicOnly)
        {
            if (filter is null) return query;

            if (filter.IsFree.HasValue)
                query = query.Where(i => i.IsFree == filter.IsFree.Value);

            if (filter.RequiresVerification.HasValue)
                query = query.Where(i => i.RequiresVerification == filter.RequiresVerification.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(i => i.IsFree || i.PricePerDay >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(i => i.IsFree || i.PricePerDay <= filter.MaxPrice.Value);

            if (filter.Condition.HasValue)
                query = query.Where(i => i.Condition == filter.Condition.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(i => i.CategoryId == filter.CategoryId.Value);

            if (filter.OwnerId != null)
                query = query.Where(i => i.OwnerId == filter.OwnerId);

            if (filter.AvailableFrom.HasValue)
                query = query.Where(i => i.AvailableFrom <= filter.AvailableFrom.Value);

            if (filter.AvailableUntil.HasValue)
                query = query.Where(i => i.AvailableUntil >= filter.AvailableUntil.Value);

            if (filter.MinLoanDays.HasValue)
                query = query.Where(i => !i.MinLoanDays.HasValue || i.MinLoanDays <= filter.MinLoanDays.Value);

            if (filter.MaxLoanDays.HasValue)
                query = query.Where(i => !i.MaxLoanDays.HasValue || i.MaxLoanDays >= filter.MaxLoanDays.Value);

            if (filter.MinRating.HasValue)
                query = query.Where(i => i.AverageRating.HasValue && i.AverageRating >= filter.MinRating.Value);

            if (filter.MaxRating.HasValue)
                query = query.Where(i => i.AverageRating.HasValue && i.AverageRating <= filter.MaxRating.Value);

            if (filter.Availability.HasValue)
                query = query.Where(i => i.Availability == filter.Availability.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(i =>
                    EF.Functions.Like(i.Title ?? "", $"%{term}%") ||
                    EF.Functions.Like(i.Description ?? "", $"%{term}%"));
            }

            //Admin-only filters — ignored when browsing publicly
            if (!publicOnly)
            {
                if (filter.Status.HasValue)
                    query = query.Where(i => i.Status == filter.Status.Value);

                if (filter.IsActive.HasValue)
                    query = query.Where(i => i.IsActive == filter.IsActive.Value);
            }

            return query;
        }

        //Haversine distance formula (km)
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; //Earth radius in km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}