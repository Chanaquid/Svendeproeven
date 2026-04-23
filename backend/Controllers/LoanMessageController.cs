using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace backend.Controllers
{
    [Route("api/loans/{loanId}/messages")]
    [ApiController]
    [Authorize]
    public class LoanMessageController : BaseController
    {
        private readonly ILoanMessageService _loanMessageService;
        private readonly IHubContext<LoanChatHub> _hubContext;

        public LoanMessageController(
            ILoanMessageService loanMessageService,
            IHubContext<LoanChatHub> hubContext)
        {
            _loanMessageService = loanMessageService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanMessageDto>>>> GetMessages(
            int loanId,
            [FromQuery] PagedRequest request)
        {
            var result = await _loanMessageService.GetMessagesAsync(loanId, Caller.UserId, Caller.IsAdmin, request);
            return Ok(ApiResponse<PagedResult<LoanMessageDto>>.Ok(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<LoanMessageDto>>> SendMessage(
            int loanId,
            [FromBody] SendLoanMessageDto dto)
        {
            var result = await _loanMessageService.SendMessageAsync(loanId, Caller.UserId, dto, Caller.IsAdmin);

            await _hubContext.Clients
                .Group($"loan_{loanId}")
                .SendAsync("ReceiveMessage", result);

            return Ok(ApiResponse<LoanMessageDto>.Ok(result));
        }

        [HttpPatch("read")]
        public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(
            int loanId,
            [FromBody] MarkLoanMessagesReadDto dto)
        {
            await _loanMessageService.MarkAsReadAsync(loanId, Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null));
        }

        [HttpGet("unread")]
        public async Task<ActionResult<ApiResponse<LoanUnreadCountDto>>> GetUnreadCount(int loanId)
        {
            var count = await _loanMessageService.GetUnreadCountAsync(loanId, Caller.UserId);
            return Ok(ApiResponse<LoanUnreadCountDto>.Ok(new LoanUnreadCountDto
            {
                LoanId = loanId,
                UnreadCount = count
            }));
        }
    }
}