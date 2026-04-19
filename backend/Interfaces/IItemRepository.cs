using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Interfaces
{
    public interface IItemRepository
    {
        //Queries
        Task<Item?> GetByIdAsync(int itemId);
        Task<Item?> GetByIdWithDetailsAsync(int itemId); //Includes Owner, Category, Photos, Reviews, Loans
        Task<Item?> GetByIdIncludingDeletedAsync(int itemId); //For admins
        Task<Item?> GetByQrCodeAsync(string qrCode);
        Task<Item?> GetBySlugAsync(string slug);
        Task<Item?> GetBySlugWithDetailsAsync(string slug);

        Task<PagedResult<Item>> GetAllApprovedAsync(ItemFilter? filter, PagedRequest request); //for public - only sees active items
        Task<PagedResult<Item>> GetAllAsAdminAsync(ItemFilter? filter, PagedRequest request); //Admin — pass true to see inactive
        Task<PagedResult<Item>> GetByOwnerIdAsync(string ownerId, ItemFilter? filter, PagedRequest request);
        Task<List<Item>> GetByOwnerIdAsync(string ownerId); //For soft delete
        Task<PagedResult<Item>> GetPublicByOwnerAsync(string ownerId, ItemFilter? filter, PagedRequest request); //Only approved + active items
        Task<PagedResult<Item>> GetByCategoryAsync(int categoryId, ItemFilter? filter, PagedRequest request);
        Task<PagedResult<Item>> GetPendingApprovalsAsync(ItemFilter? filter, PagedRequest request);
        Task<PagedResult<Item>> GetActiveItemsExpiredBeforeAsync(DateTime date, ItemFilter? filter, PagedRequest request);
        Task<PagedResult<Item>> GetAvailableItemsAsync(ItemFilter? filter, PagedRequest request); //Approved + active + Available status

        //4 items 
        Task<List<Item>> GetNewestListedAsync(int count = 4);
        Task<int> GetAvailableCountAsync();

        //Returns items within a certain radius of the user's current location
        Task<PagedResult<Item>> GetNearbyItemsAsync(double lat, double lon, double radiusKm, ItemFilter? filter, PagedRequest request);

        //Checks
        Task<bool> QrCodeExistsAsync(string qrCode);
        Task<bool> IsOwnerAsync(int itemId, string userId);

        Task<int> GetPendingApprovalsCountAsync(); //For admin dashboard


        //Photos
        Task AddPhotoAsync(ItemPhoto photo);
        Task<ItemPhoto?> GetPhotoByIdAsync(int photoId);
        void DeletePhoto(ItemPhoto photo); //Softdelete

        //CRUD
        Task AddAsync(Item item);
        void Update(Item item);
        void Delete(Item item);
        Task SaveChangesAsync();

        Task<bool> SlugExistsAsync(string slug);

        Task<IDbContextTransaction> BeginTransactionAsync();

    }



}