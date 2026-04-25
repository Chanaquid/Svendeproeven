using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IItemReviewRepository _itemReviewRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly INotificationService _notificationService;
        private readonly IUserFavoriteRepository _userFavoriteRepository;
        private readonly IUserBlockRepository _userBlockRepository;

        private readonly UserManager<ApplicationUser> _userManager;

        public ItemService(
            IItemRepository itemRepository,
            ICategoryRepository categoryRepository,
            IUserRepository userRepository,
            IItemReviewRepository itemReviewRepository,
            ILoanRepository loanRepository,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IUserFavoriteRepository userFavoriteRepository,
            IUserBlockRepository userBlockRepository)
        {
            _itemRepository = itemRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _itemReviewRepository = itemReviewRepository;
            _loanRepository = loanRepository;
            _notificationService = notificationService;
            _userManager = userManager;
            _userFavoriteRepository = userFavoriteRepository;
            _userBlockRepository = userBlockRepository;
        }

        //User
        public async Task<ItemDto> GetByIdAsync(int itemId, string? currentUserId)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            bool isAdmin = currentUserId != null &&
                await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(currentUserId), "Admin");

            if (item.OwnerId != currentUserId && !isAdmin)
            {
                if (item.Status != ItemStatus.Approved || !item.IsActive)
                    throw new KeyNotFoundException($"Item {itemId} not found");
            }

            bool isFavorited = currentUserId != null &&
                await _userFavoriteRepository.ExistsAsync(currentUserId, itemId);

            var dto = MapToItemDto(item, currentUserId);
            dto.IsFavoritedByCurrentUser = isFavorited;
            return dto;
        }

        public async Task<ItemDto> GetBySlugAsync(string slug, string? currentUserId)
        {
            var item = await _itemRepository.GetBySlugAsync(slug)
                ?? throw new KeyNotFoundException($"Item not found");

            bool isAdmin = currentUserId != null &&
                await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(currentUserId), "Admin");

            if (item.OwnerId != currentUserId && !isAdmin)
            {
                if (item.Status != ItemStatus.Approved || !item.IsActive)
                    throw new KeyNotFoundException($"Item not found");
            }

            bool isFavorited = currentUserId != null &&
                await _userFavoriteRepository.ExistsAsync(currentUserId, item.Id);

            var dto = MapToItemDto(item, currentUserId);
            dto.IsFavoritedByCurrentUser = isFavorited;
            return dto;
        }

        //Use the block check helpers and filters
        public async Task<PagedResult<ItemListDto>> GetAllApprovedAsync(ItemFilter? filter, PagedRequest request, string? currentUserId)
        {
            var blocked = await GetBlockedOwnerIdsAsync(currentUserId);  //fetch once
            var result = await _itemRepository.GetAllApprovedAsync(filter, request, blocked); 
            return MapToPagedListDto(result, currentUserId);
        }

        public async Task<PagedResult<ItemListDto>> GetAvailableItemsAsync(ItemFilter? filter, PagedRequest request, string? currentUserId)
        {
            var result = await _itemRepository.GetAvailableItemsAsync(filter, request);
            var mapped = MapToPagedListDto(result, currentUserId);
            var blocked = await GetBlockedOwnerIdsAsync(currentUserId);
            return FilterBlockedOwners(mapped, blocked);
        }

        public async Task<PagedResult<ItemListDto>> GetByCategoryAsync(int categoryId, ItemFilter? filter, PagedRequest request, string? currentUserId)
        {
            var result = await _itemRepository.GetByCategoryAsync(categoryId, filter, request);
            var mapped = MapToPagedListDto(result, currentUserId);
            var blocked = await GetBlockedOwnerIdsAsync(currentUserId);
            return FilterBlockedOwners(mapped, blocked);
        }

        public async Task<PagedResult<ItemListDto>> GetNearbyItemsAsync(double lat, double lon, double radiusKm, ItemFilter? filter, PagedRequest request, string? currentUserId)
        {
            var result = await _itemRepository.GetNearbyItemsAsync(lat, lon, radiusKm, filter, request);
            var mapped = MapToPagedListDto(result, currentUserId);
            var blocked = await GetBlockedOwnerIdsAsync(currentUserId);
            return FilterBlockedOwners(mapped, blocked);
        }

        public async Task<PagedResult<ItemListDto>> GetPublicByOwnerAsync(string ownerId, ItemFilter? filter, PagedRequest request, string? currentUserId)
        {
            var blocked = await GetBlockedOwnerIdsAsync(currentUserId);
            if (blocked.Contains(ownerId))
                return new PagedResult<ItemListDto>
                {
                    Items = new List<ItemListDto>(),
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

            var result = await _itemRepository.GetPublicByOwnerAsync(ownerId, filter, request);
            return MapToPagedListDto(result, currentUserId);
        }

        public async Task<PagedResult<ItemListDto>> GetMyItemsAsync(string ownerId, ItemFilter? filter, PagedRequest request)
        {
            var result = await _itemRepository.GetByOwnerIdAsync(ownerId, filter, request);
            return MapToPagedListDto(result, ownerId);
        }

        public async Task<List<ItemListDto>> GetNewestListedAsync(int count = 4)
        {
            var items = await _itemRepository.GetNewestListedAsync(count);

            return items.Select(i => MapToItemListDto(i, null)).ToList();
        }



        //CRUD

        public async Task<ItemDto> CreateItemAsync(string ownerId, CreateItemDto dto)
        {
            var owner = await _userRepository.GetByIdAsync(ownerId);
            if (owner == null)
                throw new KeyNotFoundException("Owner not found.");

            bool isAdmin = await _userManager.IsInRoleAsync(owner, "Admin");

            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null)
                throw new ArgumentException($"Category with ID {dto.CategoryId} does not exist.");

            if (dto.CurrentValue < 0)
                throw new ArgumentException("Current value cant be negative");

            //Normalize loan constraints: treat 0 as "not set"
            var minLoanDays = dto.MinLoanDays == 0 ? null : dto.MinLoanDays;
            var maxLoanDays = dto.MaxLoanDays == 0 ? null : dto.MaxLoanDays;

            //Normalize dates early
            var availableFrom = dto.AvailableFrom.Date;
            var availableUntil = dto.AvailableUntil.Date;

            if (availableFrom < DateTime.Today)
                throw new ArgumentException("The availability start date cannot be in the past.");

            if (availableFrom >= availableUntil)
                throw new ArgumentException("AvailableFrom must be before AvailableUntil.");

            //Loan constraints consistency
            if (minLoanDays.HasValue && maxLoanDays.HasValue &&
                minLoanDays > maxLoanDays)
                throw new ArgumentException("MinLoanDays cannot exceed MaxLoanDays.");

            var availableDays = (availableUntil - availableFrom).TotalDays;


            if (availableDays <= 0)
                throw new ArgumentException("Availability window must be at least 1 day.");

            if (minLoanDays.HasValue && availableDays < minLoanDays.Value)
                throw new ArgumentException(
                    $"Availability window ({availableDays:0} days) must be at least equal to MinLoanDays ({minLoanDays}).");

            if (maxLoanDays.HasValue && availableDays < maxLoanDays.Value)
                throw new ArgumentException(
                    $"Availability window ({availableDays:0} days) must allow MaxLoanDays ({maxLoanDays}).");

            //Fall back to user's location if pickup details are not provided
            var pickupAddress = !string.IsNullOrWhiteSpace(dto.PickupAddress)
                ? dto.PickupAddress.Trim()
                : owner.Address ?? throw new ArgumentException("Pickup address is required and no address is set on your profile.");

            var pickupLat = dto.PickupLatitude != 0 ? dto.PickupLatitude
                : owner.Latitude ?? throw new ArgumentException("Pickup location is required and no location is set on your profile.");

            var pickupLong = dto.PickupLongitude != 0 ? dto.PickupLongitude
                : owner.Longitude ?? throw new ArgumentException("Pickup location is required and no location is set on your profile.");


            var item = new Item
            {
                OwnerId = ownerId,
                CategoryId = dto.CategoryId,
                Title = dto.Title.Trim(),
                Slug = await GenerateUniqueSlugAsync(dto.Title),//URL-friendly unique identifier
                Description = dto.Description.Trim(),
                CurrentValue = dto.CurrentValue,
                PricePerDay = dto.IsFree ? 0 : dto.PricePerDay,
                IsFree = dto.IsFree,
                Condition = dto.Condition,
                QrCode = await GenerateUniqueQrCodeAsync(),//Unique string for physical item tracking
                MinLoanDays = minLoanDays,
                MaxLoanDays = maxLoanDays,
                RequiresVerification = dto.RequiresVerification,
                PickupAddress = pickupAddress,
                PickupLatitude = pickupLat,
                PickupLongitude = pickupLong,
                AvailableFrom = dto.AvailableFrom.ToUniversalTime(),
                AvailableUntil = dto.AvailableUntil.ToUniversalTime(),
                Status = isAdmin ? ItemStatus.Approved : ItemStatus.Pending,
                Availability = ItemAvailability.Available,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(item);
            await _itemRepository.SaveChangesAsync();

            await _notificationService.SendToAdminsAsync(
                NotificationType.ItemPendingReview,
                $"New item '{item.Title}' is pending approval.",
                item.Id,
                NotificationReferenceType.Item);

            return await GetByIdAsync(item.Id, ownerId);
        }

        public async Task<ItemDto> UpdateItemAsync(string ownerId, int itemId, UpdateItemDto dto)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found.");

            var user = await _userManager.FindByIdAsync(ownerId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");


            if (item.OwnerId != ownerId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to edit this item");

            var categoryId = dto.CategoryId ?? item.CategoryId;

            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException($"Category with ID {dto.CategoryId} does not exist.");
            }

            if (dto.CurrentValue.HasValue && dto.CurrentValue.Value < 0)
                throw new ArgumentException("Current value cannot be negative");


            var availableFrom = dto.AvailableFrom ?? item.AvailableFrom;
            var availableUntil = dto.AvailableUntil ?? item.AvailableUntil;

            if (availableFrom >= availableUntil)
                throw new ArgumentException("AvailableFrom must be before AvailableUntil.");


            //minimum loan days must be smaller than the whole loan window
            if (dto.MinLoanDays.HasValue)
            {
                var availableDays = (int)(availableUntil - availableFrom).TotalDays;

                if (availableDays < dto.MinLoanDays.Value)
                    throw new ArgumentException(
                        $"Availability window ({availableDays} days) must be >= MinLoanDays ({dto.MinLoanDays}).");
            }

            if (dto.MaxLoanDays.HasValue)
            {
                var availableDays = (int)(availableUntil - availableFrom).TotalDays;

                if (availableDays < dto.MaxLoanDays.Value)
                    throw new ArgumentException(
                        $"Availability window ({availableDays} days) must allow MaxLoanDays ({dto.MaxLoanDays}).");
            }


            if (dto.Title != null)
            {
                item.Title = dto.Title.Trim();
                item.Slug = await GenerateUniqueSlugAsync(dto.Title, itemId);
            }

            item.CategoryId = categoryId;
            if (dto.Description != null) item.Description = dto.Description.Trim();
            if (dto.CurrentValue.HasValue) item.CurrentValue = dto.CurrentValue.Value;
            if (dto.Condition.HasValue) item.Condition = dto.Condition.Value;
            if (dto.MinLoanDays.HasValue) item.MinLoanDays = dto.MinLoanDays;
            if (dto.MaxLoanDays.HasValue) item.MaxLoanDays = dto.MaxLoanDays;
            if (dto.RequiresVerification.HasValue) item.RequiresVerification = dto.RequiresVerification.Value;
            if (dto.PickupAddress != null) item.PickupAddress = dto.PickupAddress.Trim();
            if (dto.PickupLatitude.HasValue) item.PickupLatitude = dto.PickupLatitude.Value;
            if (dto.PickupLongitude.HasValue) item.PickupLongitude = dto.PickupLongitude.Value;
            if (dto.AvailableFrom.HasValue) item.AvailableFrom = dto.AvailableFrom.Value.ToUniversalTime();
            if (dto.AvailableUntil.HasValue) item.AvailableUntil = dto.AvailableUntil.Value.ToUniversalTime();
            if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
            if (dto.Availability.HasValue) item.Availability = dto.Availability.Value;

            if (dto.IsFree.HasValue)
            {
                item.IsFree = dto.IsFree.Value;
                item.PricePerDay = item.IsFree ? 0 : (dto.PricePerDay ?? item.PricePerDay);
            }
            else if (dto.PricePerDay.HasValue)
            {
                item.PricePerDay = dto.PricePerDay.Value;
            }

            item.UpdatedAt = DateTime.UtcNow;
            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            return await GetByIdAsync(item.Id, ownerId);
        }


        //SoftDelete
        public async Task DeleteItemAsync(string userId, int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            var user = await _userManager.FindByIdAsync(userId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (item.OwnerId != userId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to delete this item");

            if (await _loanRepository.HasActiveOrApprovedLoansByItemIdAsync(itemId))
                throw new InvalidOperationException("Cannot delete an item with active or approved loans.");

            //Get all users who favorited this item before removing
            var favorites = await _userFavoriteRepository.GetAllByItemIdAsync(itemId);
            var userIdsToNotify = favorites
                .Select(f => f.UserId)
                .ToList();

            await using var transaction = await _itemRepository.BeginTransactionAsync();
            try
            {
                foreach (var photo in item.Photos.ToList())
                {
                    _itemRepository.DeletePhoto(photo);
                }


                _itemReviewRepository.MarkReviewsDeletedByItemId(itemId);
                _userFavoriteRepository.RemoveRange(favorites);

                SoftDelete(item, user!);
                _itemRepository.Update(item);

                await _itemRepository.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }


            var notifyTasks = userIdsToNotify.Select(favUserId =>
                _notificationService.SendAsync(
                    favUserId,
                    NotificationType.ItemDeleted,
                    $"An item {item.Title} you saved is no longer available.",
                    itemId,
                    NotificationReferenceType.Item));

            await Task.WhenAll(notifyTasks);
        
        }

        public async Task<ItemDto> ToggleActiveStatusAsync(string ownerId, int itemId, bool isActive)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            var user = await _userManager.FindByIdAsync(ownerId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");


            if (item.OwnerId != ownerId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to modify this item.");

            item.IsActive = isActive;
            item.UpdatedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            return await GetByIdAsync(item.Id, ownerId);
        }

        public async Task<ItemQrCodeDto> GetQrCodeAsync(string requesterId, int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            var user = await _userManager.FindByIdAsync(requesterId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (item.OwnerId != requesterId && !isAdmin)
                throw new UnauthorizedAccessException("Only the owner or an admin can view the QR code");

            return new ItemQrCodeDto { ItemId = item.Id, QrCode = item.QrCode };
        }


        //Photo management
        public async Task<ItemDto> AddPhotoAsync(string ownerId, int itemId, AddItemPhotoDto dto)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found.");

            var user = await _userManager.FindByIdAsync(ownerId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (item.OwnerId != ownerId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to modify this item");


            if (dto.IsPrimary)
            {
                foreach (var p in item.Photos.Where(x => x.IsPrimary)) p.IsPrimary = false;
            }

            bool isFirstPhoto = !item.Photos.Any();

            var photo = new ItemPhoto
            {
                ItemId = itemId,
                PhotoUrl = dto.PhotoUrl,
                IsPrimary = dto.IsPrimary || isFirstPhoto,
                DisplayOrder = dto.DisplayOrder,
                UploadedAt = DateTime.UtcNow
            };

            await _itemRepository.AddPhotoAsync(photo);
            await _itemRepository.SaveChangesAsync();

            return await GetByIdAsync(itemId, ownerId);
        }

        public async Task<ItemDto> DeletePhotoAsync(string ownerId, int itemId, int photoId)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found.");

            var user = await _userManager.FindByIdAsync(ownerId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (item.OwnerId != ownerId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to modify this item");


            var photo = item.Photos.FirstOrDefault(p => p.Id == photoId)
                ?? throw new KeyNotFoundException("Photo not found");

            bool wasPrimary = photo.IsPrimary;

            item.Photos.Remove(photo);

            _itemRepository.DeletePhoto(photo);

            if (wasPrimary && item.Photos.Any())
            {
                var nextPrimary = item.Photos
                    .OrderBy(p => p.DisplayOrder)
                    .First();

                nextPrimary.IsPrimary = true;
            }

            await _itemRepository.SaveChangesAsync();

            return await GetByIdAsync(itemId, ownerId);
        }

        public async Task<ItemDto> SetPrimaryPhotoAsync(string ownerId, int itemId, int photoId)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found.");

            var user = await _userManager.FindByIdAsync(ownerId);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (item.OwnerId != ownerId && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to modify this item");


            foreach (var p in item.Photos)
            {
                p.IsPrimary = (p.Id == photoId);
            }

            await _itemRepository.SaveChangesAsync();
            return await GetByIdAsync(itemId, ownerId);
        }

        //ADmin
        public async Task<ItemDto> AdminGetByIdAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            return MapToItemDto(item, null);
        }

        public async Task<PagedResult<ItemListDto>> AdminGetAllAsync(ItemFilter? filter, PagedRequest request)
        {
            var result = await _itemRepository.GetAllAsAdminAsync(filter, request);
            return MapToPagedListDto(result, null);
        }

        public async Task<PagedResult<ItemListDto>> GetPendingApprovalsAsync(ItemFilter? filter, PagedRequest request)
        {
            var result = await _itemRepository.GetPendingApprovalsAsync(filter, request);
            return MapToPagedListDto(result, null);
        }

        public async Task<ItemDto> DecideItemAsync(string adminId, int itemId, AdminDecideItemDto dto)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            if (item.Status != ItemStatus.Pending)
                throw new InvalidOperationException("Only pending items can be approved or rejected");

            item.Status = dto.IsApproved ? ItemStatus.Approved : ItemStatus.Rejected;
            item.AdminNote = dto.AdminNote;
            item.ReviewedByAdminId = adminId;
            item.ReviewedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                item.OwnerId,
                dto.IsApproved ? NotificationType.ItemApproved : NotificationType.ItemRejected,
                dto.IsApproved ? $"Item '{item.Title}' approved." : $"Item '{item.Title}' rejected: {dto.AdminNote}",
                item.Id,
                NotificationReferenceType.Item);

            return await AdminGetByIdAsync(itemId);
        }

        public async Task<ItemDto> AdminUpdateStatusAsync(string adminId, int itemId, AdminUpdateItemStatusDto dto)
        {
            var item = await _itemRepository.GetByIdAsync(itemId) ??
                throw new KeyNotFoundException($"Item {itemId} not found");

            item.Status = dto.Status;
            item.AdminNote = dto.AdminNote;
            item.ReviewedByAdminId = adminId;
            item.ReviewedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            // Send notification to owner
            var (notifType, message) = dto.Status switch
            {
                ItemStatus.Approved => (
                    NotificationType.ItemApproved,
                    $"Your item '{item.Title}' has been approved and is now live."),
                ItemStatus.Rejected => (
                    NotificationType.ItemRejected,
                    $"Your item '{item.Title}' was rejected.{(!string.IsNullOrWhiteSpace(dto.AdminNote) ? $" Reason: {dto.AdminNote}" : "")}"),
                ItemStatus.Pending => (
                    NotificationType.ItemPendingReview,
                    $"Your item '{item.Title}' has been moved back to pending review."),
                _ => ((NotificationType?)null, (string?)null)
            };

            if (notifType.HasValue && message != null)
            {
                await _notificationService.SendAsync(
                    item.OwnerId,
                    notifType.Value,
                    message,
                    item.Id,
                    NotificationReferenceType.Item);
            }

            return await AdminGetByIdAsync(itemId);
        }

        public async Task<int> GetPendingApprovalsCountAsync()
        {
            return await _itemRepository.GetPendingApprovalsCountAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return (await _itemRepository.GetBySlugAsync(slug)) != null;
        }
        public async Task<int> GetAvailableCountAsync()
        {
            return await _itemRepository.GetAvailableCountAsync();
        }

        //Helpers
        private async Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null)
        {
            var slug = title.ToLower().Trim().Replace(" ", "-");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
            var baseSlug = slug;
            int counter = 1;

            while (true)
            {
                var existing = await _itemRepository.GetBySlugAsync(slug);
                if (existing == null || existing.Id == excludeId) return slug;
                slug = $"{baseSlug}-{counter++}";
            }
        }

        private async Task<string> GenerateUniqueQrCodeAsync()
        {
            string qr;
            do { qr = Guid.NewGuid().ToString("N")[..12].ToUpper(); }
            while (await _itemRepository.QrCodeExistsAsync(qr));
            return qr;
        }

        private static bool HasSignificantChanges(UpdateItemDto dto)
        {
            return
                dto.Title != null ||
                dto.Description != null ||
                dto.PricePerDay.HasValue ||
                dto.IsFree.HasValue ||
                dto.CurrentValue.HasValue ||
                dto.Condition.HasValue ||
                dto.RequiresVerification.HasValue ||
                dto.CategoryId.HasValue;
        }

        //Dont show items listed by blocked users
        private async Task<HashSet<string>> GetBlockedOwnerIdsAsync(string? currentUserId)
        {
            if (string.IsNullOrEmpty(currentUserId))
                return new HashSet<string>();

            return await _userBlockRepository.GetBlockedUserIdsAsync(currentUserId);
        }

        private static PagedResult<ItemListDto> FilterBlockedOwners(
        PagedResult<ItemListDto> result,
        HashSet<string> blockedIds)
        {
            if (blockedIds.Count == 0) return result;

            var filtered = result.Items
                .Where(i => !blockedIds.Contains(i.OwnerId))
                .ToList();

            return new PagedResult<ItemListDto>
            {
                Items = filtered,
                TotalCount = filtered.Count, //recount after filter
                Page = result.Page,
                PageSize = result.PageSize
            };
        }



        private static ItemDto MapToItemDto(Item item, string? currentUserId)
        {
            return new ItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Description = item.Description,
                CurrentValue = item.CurrentValue,
                PricePerDay = item.PricePerDay,
                IsFree = item.IsFree,
                Condition = item.Condition,
                IsCurrentlyOnLoan = item.Loans.Any(l => l.Status == LoanStatus.Active),
                IsMine = currentUserId != null && item.OwnerId == currentUserId,
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name ?? "",
                CategorySlug = item.Category?.Slug ?? "",
                CategoryIcon = item.Category?.Icon ?? "",
                OwnerId = item.OwnerId,
                OwnerName = item.Owner?.FullName ?? "",
                OwnerUserName = item.Owner?.UserName ?? "",
                OwnerAvatarUrl = item.Owner?.AvatarUrl,
                OwnerScore = item.Owner?.Score ?? 0,
                IsOwnerVerified = item.Owner?.IsVerified ?? false,
                PickupAddress = item.PickupAddress,
                PickupLatitude = item.PickupLatitude,
                PickupLongitude = item.PickupLongitude,
                AvailableFrom = item.AvailableFrom,
                AvailableUntil = item.AvailableUntil,
                Status = item.Status,
                Availability = item.Availability,
                IsActive = item.IsActive,
                AdminNote = item.AdminNote,
                ReviewedByAdminId = item.ReviewedByAdminId,
                ReviewedByAdminName = item.ReviewedByAdmin?.FullName,
                ReviewedByAdminUserName = item.ReviewedByAdmin?.UserName,
                ReviewedByAdminAvatarUrl = item.ReviewedByAdmin?.AvatarUrl,
                MinLoanDays = item.MinLoanDays,
                MaxLoanDays = item.MaxLoanDays,
                ReviewedAt = item.ReviewedAt,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                AverageRating = item.Reviews.Any() ? Math.Round(item.Reviews.Average(r => r.Rating), 1) : null,
                ReviewCount = item.Reviews.Count,
                TotalLoans = item.Loans.Count,
                Photos = item.Photos.OrderBy(p => p.DisplayOrder).Select(p => new ItemPhotoDto
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    IsPrimary = p.IsPrimary,
                    DisplayOrder = p.DisplayOrder
                }).ToList()
            };
        }

        private static ItemListDto MapToItemListDto(Item item, string? currentUserId)
        {
            var primary = item.Photos.FirstOrDefault(p => p.IsPrimary) ?? item.Photos.FirstOrDefault();
            return new ItemListDto
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Description = item.Description,
                MainPhotoUrl = primary?.PhotoUrl,
                PricePerDay = item.PricePerDay,
                IsFree = item.IsFree,
                Status = item.Status,
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name ?? "",
                CategorySlug = item.Category?.Slug ?? "",
                Condition = item.Condition,
                Availability = item.Availability,
                PickupAddress = item.PickupAddress,
                PickupLatitude = item.PickupLatitude,
                PickupLongitude = item.PickupLongitude,
                IsActive = item.IsActive,
                AverageRating = item.Reviews?.Any() == true
                            ? Math.Round(item.Reviews.Average(r => r.Rating), 1)
                            : null,
                TotalReviews = item.Reviews?.Count ?? 0,
                OwnerId = item.OwnerId,
                OwnerName = item.Owner?.FullName ?? "",
                OwnerUsername = item.Owner?.UserName ?? "",
                OwnerAvatarUrl = item.Owner?.AvatarUrl,
                OwnerScore = item.Owner?.Score ?? 0,
                IsOwnerVerified = item.Owner?.IsVerified ?? false,
                RequiresVerification = item.RequiresVerification,
                AvailableFrom = item.AvailableFrom,
                AvailableUntil = item.AvailableUntil,
                MaxLoanDays = item.MaxLoanDays,
                MinLoanDays = item.MinLoanDays,
                CreatedAt = item.CreatedAt
            };
        }

        private static PagedResult<ItemListDto> MapToPagedListDto(PagedResult<Item> result, string? currentUserId)
        {
            return new PagedResult<ItemListDto>
            {
                Items = result.Items.Select(i => MapToItemListDto(i, currentUserId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        private static void SoftDelete(Item item, ApplicationUser user)
        {
            var suffix = DateTime.UtcNow.Ticks.ToString();

            item.Title = $"deleted_item_{suffix}";
            item.Description = "This item has been deleted.";

            item.IsActive = false;
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.DeletedByUserId = user.Id;

            item.Status = ItemStatus.Deleted;
            item.Availability = ItemAvailability.Unavailable;

            item.PickupAddress = "Deleted";
            item.PickupLatitude = 0;
            item.PickupLongitude = 0;

            item.PricePerDay = 0;
            item.CurrentValue = 0;

            item.Slug = $"deleted_item_slug-{suffix}";
            item.QrCode = $"D{suffix}"[..12];

            item.MinLoanDays = null;
            item.MaxLoanDays = null;

            item.AvailableFrom = DateTime.UtcNow;  //Not nullable so
            item.AvailableUntil = DateTime.UtcNow; 
        }


    }
}