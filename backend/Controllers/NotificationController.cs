using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        //GET: api/notifications/summary
        //Unread count + last 10 — used for the notification bell in the navbar
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<NotificationSummaryDto>>> GetSummary()
        {
            var result = await _notificationService.GetSummaryAsync(Caller.UserId);
            return Ok(ApiResponse<NotificationSummaryDto>.Ok(result));
        }

        //GET: api/notifications
        //Full pagedlist — when user opens their notification panel
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetAll(
            [FromQuery] NotificationFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _notificationService.GetAllAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result));
        }
        //PATCH: api/notifications/{id}/read
        [HttpPatch("{id:int}/read")]
        public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Notification marked as read."));
        }

        //PATCH: api/notifications/read-multiple
        //Mark a specific set of notifications as read
        [HttpPatch("read-multiple")]
        public async Task<ActionResult<ApiResponse<string>>> MarkMultipleAsRead(
            [FromBody] MarkMultipleNotificationsReadDto dto)
        {
            await _notificationService.MarkMultipleAsReadAsync(dto.NotificationIds, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Notifications marked as read."));
        }

        //PATCH: api/notifications/read-all
        [HttpPatch("read-all")]
        public async Task<ActionResult<ApiResponse<string>>> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "All notifications marked as read."));
        }

        //DELETE: api/notifications/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            await _notificationService.DeleteAsync(id, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Notification deleted."));
        }

        //DELETE: api/notifications/all
        [HttpDelete("all")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAll()
        {
            await _notificationService.DeleteAllAsync(Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "All notifications deleted."));
        }
    }
}