using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    [Authorize]
    public class ConversationController : BaseController
    {
        private readonly IDirectConversationService _conversationService;
        private readonly IDirectMessageService _messageService;

        public ConversationController(
            IDirectConversationService conversationService,
            IDirectMessageService messageService)
        {
            _conversationService = conversationService;
            _messageService = messageService;
        }


        [HttpPost("with/{otherUserId}")]
        public async Task<ActionResult<ApiResponse<DirectConversationDto>>>
            GetOrCreateConversation(string otherUserId)
        {
            var result = await _conversationService.GetOrCreateConversationAsync(
                Caller.UserId,
                otherUserId);

            return Ok(ApiResponse<DirectConversationDto>.Ok(result, "Conversation ready"));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<DirectConversationListDto>>>>
            GetMyConversations([FromQuery] ConversationFilter? filter, [FromQuery] PagedRequest request)
        {
            var result = await _conversationService.GetUserConversationsAsync(
                Caller.UserId,
                filter,
                request);

            return Ok(ApiResponse<PagedResult<DirectConversationListDto>>.Ok(result));
        }

        [HttpGet("{conversationId}")]
        public async Task<ActionResult<ApiResponse<DirectConversationDto>>>
            GetConversation(int conversationId)
        {
            var result = await _conversationService.GetConversationAsync(
                conversationId,
                Caller.UserId);

            return Ok(ApiResponse<DirectConversationDto>.Ok(result));
        }

        [HttpDelete("{conversationId}")]
        public async Task<ActionResult<ApiResponse<string>>>
            DeleteConversation(int conversationId)
        {
            await _conversationService.DeleteConversationForUserAsync(
                conversationId,
                Caller.UserId);

            return Ok(ApiResponse<string>.Ok(null, "Conversation deleted"));
        }

        [HttpPost("{conversationId}/restore")]
        public async Task<ActionResult<ApiResponse<string>>>
            RestoreConversation(int conversationId)
        {
            await _conversationService.RestoreConversationForUserAsync(
                conversationId,
                Caller.UserId);

            return Ok(ApiResponse<string>.Ok(null, "Conversation restored"));
        }

        //Message endpoints


        [HttpPost("{conversationId}/messages")]
        public async Task<ActionResult<ApiResponse<DirectMessageDto>>>
            SendMessage(int conversationId, [FromBody] SendDirectMessageDto dto)
        {

            var result = await _messageService.SendMessageAsync(
                conversationId,
                Caller.UserId,
                dto.Content);

            return Ok(ApiResponse<DirectMessageDto>.Ok(result, "Message sent"));
        }

        [HttpGet("{conversationId}/messages")]
        public async Task<ActionResult<ApiResponse<PagedResult<DirectMessageDto>>>>
            GetMessages(
                int conversationId,
                [FromQuery] MessageFilter? filter,
                [FromQuery] PagedRequest request)
        {
            var result = await _messageService.GetConversationMessagesAsync(
                conversationId,
                Caller.UserId,
                filter,
                request);

            return Ok(ApiResponse<PagedResult<DirectMessageDto>>.Ok(result));
        }

        [HttpPost("{conversationId}/read")]
        public async Task<ActionResult<ApiResponse<string>>>
            MarkAsRead(int conversationId)
        {
            await _messageService.MarkMessagesAsReadAsync(conversationId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Messages marked as read"));
        }


        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>>
            GetTotalUnreadCount()
        {
            var result = await _conversationService.GetTotalUnreadCountAsync(Caller.UserId);
            return Ok(ApiResponse<int>.Ok(result));
        }

        [HttpGet("unread-counts")]
        public async Task<ActionResult<ApiResponse<UnreadCountsDto>>>
            GetUnreadCountsPerConversation()
        {
            var result = await _conversationService.GetUnreadCountsPerConversationAsync(Caller.UserId);
            return Ok(ApiResponse<UnreadCountsDto>.Ok(result));
        }
    }
}