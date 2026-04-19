using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IItemService
    {
        //User
        Task<ItemDto> GetByIdAsync(int itemId, string? currentUserId);
        Task<ItemDto> GetBySlugAsync(string slug, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetAllApprovedAsync(ItemFilter? filter, PagedRequest request, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetAvailableItemsAsync(ItemFilter? filter, PagedRequest request, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetByCategoryAsync(int categoryId, ItemFilter? filter, PagedRequest request, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetNearbyItemsAsync(double lat, double lon, double radiusKm, ItemFilter? filter, PagedRequest request, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetPublicByOwnerAsync(string ownerId, ItemFilter? filter, PagedRequest request, string? currentUserId);
        Task<PagedResult<ItemListDto>> GetMyItemsAsync(string ownerId, ItemFilter? filter, PagedRequest request);
        Task<ItemDto> CreateItemAsync(string ownerId, CreateItemDto dto);
        Task<ItemDto> UpdateItemAsync(string ownerId, int itemId, UpdateItemDto dto);
        Task DeleteItemAsync(string ownerId, int itemId);
        Task<ItemDto> ToggleActiveStatusAsync(string ownerId, int itemId, bool isActive);
        Task<ItemQrCodeDto> GetQrCodeAsync(string requesterId, int itemId);

        //For frontend landing page
        Task<List<ItemListDto>> GetNewestListedAsync(int count = 4);
        Task<int> GetAvailableCountAsync();

        //Photos
        Task<ItemDto> AddPhotoAsync(string ownerId, int itemId, AddItemPhotoDto dto);
        Task<ItemDto> DeletePhotoAsync(string ownerId, int itemId, int photoId);
        Task<ItemDto> SetPrimaryPhotoAsync(string ownerId, int itemId, int photoId);

        //Admin
        Task<ItemDto> AdminGetByIdAsync(int itemId);
        Task<PagedResult<ItemListDto>> AdminGetAllAsync(ItemFilter? filter, PagedRequest request);
        Task<PagedResult<ItemListDto>> GetPendingApprovalsAsync(ItemFilter? filter, PagedRequest request);
        Task<ItemDto> DecideItemAsync(string adminId, int itemId, AdminDecideItemDto dto);
        Task<ItemDto> AdminUpdateStatusAsync(string adminId, int itemId, AdminUpdateItemStatusDto dto);
        Task<int> GetPendingApprovalsCountAsync();


        Task<bool> SlugExistsAsync(string slug);
    }
}