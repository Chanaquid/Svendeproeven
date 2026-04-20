using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Interfaces
{
    public interface ILoanService
    {
        //Borrower
        Task<LoanDto> CreateLoanAsync(string borrowerId, CreateLoanDto dto);
        Task<LoanDto> CancelLoanAsync(string borrowerId, int loanId, string? reason);
        Task<LoanDto> RequestExtensionAsync(string borrowerId, int loanId, RequestExtensionDto dto);

        //Owner/lender
        Task<LoanDto> DecideLoanAsync(string ownerId, int loanId, OwnerDecideLoanDto dto);
        Task<LoanDto> DecideExtensionAsync(string ownerId, int loanId, DecideExtensionDto dto);

        //QR
        Task<LoanDto> ConfirmPickupAsync(string scannerId, ScanQrCodeDto dto);
        Task<LoanDto> ConfirmReturnAsync(string scannerId, ScanQrCodeDto dto);

        //Admin
        Task<LoanDto> AdminReviewLoanAsync(string adminId, int loanId, AdminReviewLoanDto dto);
        Task<LoanDto> AdminGetByIdAsync(int loanId);
        Task<PagedResult<LoanListDto>> AdminGetAllAsync(LoanFilter? filter, PagedRequest request);
        Task<List<AdminPendingLoanDto>> GetPendingAdminApprovalsAsync();
        Task<int> GetPendingAdminApprovalsCountAsync();
        Task<int> GetActiveLoansCountAsync();

        //Queries
        Task<LoanDto> GetByIdAsync(int loanId, string currentUserId);
        Task<PagedResult<LoanListDto>> GetMyLoansAsBorrowerAsync(string borrowerId, LoanFilter? filter, PagedRequest request);
        Task<PagedResult<LoanListDto>> GetMyLoansAsLenderAsync(string lenderId, LoanFilter? filter, PagedRequest request);

        Task<PagedResult<LoanListDto>> GetAllLoansByUserIdAsync(
            string userId,
            LoanFilter? filter,
            PagedRequest request,
            bool isAdmin = false);

        Task<LoanDto?> GetMyActiveLoanForItemAsync(string userId, int itemId);

    }
}