using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/loans")]
    [Authorize]
    public class LoanController : BaseController
    {
        private readonly ILoanService _loanService;

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        //Borrower

        //POST: api/loans
        [HttpPost]
        public async Task<ActionResult<ApiResponse<LoanDto>>> CreateLoan([FromBody] CreateLoanDto dto)
        {
            var loan = await _loanService.CreateLoanAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetById), new { id = loan.Id },
                ApiResponse<LoanDto>.Ok(loan, "Loan request submitted successfully."));
        }

        //POST: api/loans/{id}/cancel
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> CancelLoan(
            int id,
            [FromBody] CancelLoanDto dto)
        {
            var loan = await _loanService.CancelLoanAsync(Caller.UserId, id, dto.Reason);
            return Ok(ApiResponse<LoanDto>.Ok(loan, "Loan cancelled successfully."));
        }

        //POST: api/loans/{id}/request-extension
        [HttpPost("{id:int}/request-extension")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> RequestExtension(
            int id,
            [FromBody] RequestExtensionDto dto)
        {
            var loan = await _loanService.RequestExtensionAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<LoanDto>.Ok(loan, "Extension request submitted."));
        }


        //POST: api/loans/pickup — borrower or owner scans QR to confirm pickup
        [HttpPost("pickup")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> ConfirmPickup([FromBody] ScanQrCodeDto dto)
        {
            var loan = await _loanService.ConfirmPickupAsync(Caller.UserId, dto);
            return Ok(ApiResponse<LoanDto>.Ok(loan, "Pickup confirmed. Loan is now active."));
        }

        //POST: api/loans/return — borrower or owner scans QR to confirm return
        [HttpPost("return")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> ConfirmReturn([FromBody] ScanQrCodeDto dto)
        {
            var loan = await _loanService.ConfirmReturnAsync(Caller.UserId, dto);
            return Ok(ApiResponse<LoanDto>.Ok(loan, "Return confirmed. Loan completed."));
        }

        //Owner

        //POST: api/loans/{id}/decide — owner approves or rejects a loan request
        [HttpPost("{id:int}/decide")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> DecideLoan(
            int id,
            [FromBody] OwnerDecideLoanDto dto)
        {
            var loan = await _loanService.DecideLoanAsync(Caller.UserId, id, dto);
            var message = dto.IsApproved ? "Loan approved." : "Loan rejected.";
            return Ok(ApiResponse<LoanDto>.Ok(loan, message));
        }

        //POST: api/loans/{id}/decide-extension — owner approves or rejects an extension
        [HttpPost("{id:int}/decide-extension")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> DecideExtension(
            int id,
            [FromBody] DecideExtensionDto dto)
        {
            var loan = await _loanService.DecideExtensionAsync(Caller.UserId, id, dto);
            var message = dto.IsApproved ? "Extension approved." : "Extension rejected.";
            return Ok(ApiResponse<LoanDto>.Ok(loan, message));
        }



        // GET: api/loans/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> GetById(int id)
        {
            var loan = await _loanService.GetByIdAsync(id, Caller.UserId);
            return Ok(ApiResponse<LoanDto>.Ok(loan));
        }

        //GET: api/loans/my/borrowing — loans where current user is the borrower
        [HttpGet("my/borrowing")]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanListDto>>>> GetMyLoansAsBorrower(
            [FromQuery] LoanFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _loanService.GetMyLoansAsBorrowerAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<LoanListDto>>.Ok(result));
        }

        //GET: api/loans/my/lending — loans where current user is the owner
        [HttpGet("my/lending")]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanListDto>>>> GetMyLoansAsLender(
            [FromQuery] LoanFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _loanService.GetMyLoansAsLenderAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<LoanListDto>>.Ok(result));
        }

        //Admin

        // GET: api/loans/admin/all
        [HttpGet("admin/all")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanListDto>>>> AdminGetAll(
            [FromQuery] LoanFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _loanService.AdminGetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<LoanListDto>>.Ok(result));
        }

        // GET: api/loans/admin/{id}
        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<LoanDto>>> AdminGetById(int id)
        {
            var loan = await _loanService.AdminGetByIdAsync(id);
            return Ok(ApiResponse<LoanDto>.Ok(loan));
        }

        // GET: api/loans/admin/pending — low-score loans awaiting admin review
        [HttpGet("admin/pending")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<List<AdminPendingLoanDto>>>> GetPendingAdminApprovals()
        {
            var result = await _loanService.GetPendingAdminApprovalsAsync();
            return Ok(ApiResponse<List<AdminPendingLoanDto>>.Ok(result));
        }

        // GET: api/loans/admin/pending/count
        [HttpGet("admin/pending/count")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<int>>> GetPendingAdminApprovalsCount()
        {
            var count = await _loanService.GetPendingAdminApprovalsCountAsync();
            return Ok(ApiResponse<int>.Ok(count));
        }

        // POST: api/loans/admin/{id}/review — admin approves or rejects a low-score loan
        [HttpPost("admin/{id:int}/review")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<LoanDto>>> AdminReviewLoan(
            int id,
            [FromBody] AdminReviewLoanDto dto)
        {
            var loan = await _loanService.AdminReviewLoanAsync(Caller.UserId, id, dto);
            var message = dto.IsApproved ? "Loan forwarded to owner." : "Loan rejected.";
            return Ok(ApiResponse<LoanDto>.Ok(loan, message));
        }
    }
}