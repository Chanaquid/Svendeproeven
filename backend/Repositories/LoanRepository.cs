using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly ApplicationDbContext _context;

        public LoanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(LoanFilter filter)
        {
            var query = _context.Loans.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            return await query.CountAsync();
        }


        //Status group used for "ongoing loan" checks
        private static readonly LoanStatus[] OngoingStatuses =
        {
            LoanStatus.Approved,
            LoanStatus.Active,
            LoanStatus.Late
        };


        public async Task<Loan?> GetByIdAsync(int loanId)
        {
            return await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }


        //Get loan with full related data (used in detail views)
        public async Task<Loan?> GetByIdWithDetailsAsync(int loanId)
        {
            return await _context.Loans
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(l => l.Item)
                    .ThenInclude(i => i.Photos)
                .Include(l => l.Borrower)
                .Include(l => l.Lender)
                .Include(l => l.AdminReviewer)
                .Include(l => l.OwnerApprover)
                .Include(l => l.Fines)
                .Include(l => l.SnapshotPhotos)
                .Include(l => l.Disputes)
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }

        public async Task<Loan?> GetActiveLoanByItemIdAsync(int itemId)
        {
            return await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.ItemId == itemId && l.Status == LoanStatus.Active);
        }


        //List queries — internal use 

        public async Task<List<Loan>> GetByBorrowerIdAsync(string borrowerId)
        {
            return await _context.Loans
                .Where(l => l.BorrowerId == borrowerId)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Loans
                .Where(l => l.LenderId == ownerId)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetAllAsync()
        {
            return await _context.Loans.ToListAsync();
        }

        public async Task<List<Loan>> GetPendingAdminApprovalsAsync()
        {
            return await _context.Loans
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(l => l.Item)
                    .ThenInclude(i => i.Photos)
                .Include(l => l.Borrower)
                .Include(l => l.Lender)
                .Where(l => l.Status == LoanStatus.AdminPending)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetActiveAndOverdueAsync()
        {
            return await _context.Loans
                .IgnoreQueryFilters()
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .Include(l => l.Lender)
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Late)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetByStatusAsync(LoanStatus status)
        {
            return await _context.Loans
                .Where(l => l.Status == status)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetLoanHistoryByItemIdAsync(int itemId)
        {
            return await _context.Loans
                .IgnoreQueryFilters()
                .Include(l => l.Borrower)
                .Where(l => l.ItemId == itemId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Loan>> GetExpiredPendingLoansAsync(DateTime cutoff)
        {
            return await _context.Loans
                .Where(l => (l.Status == LoanStatus.Pending ||
                             l.Status == LoanStatus.AdminPending)
                            && l.CreatedAt < cutoff)
                .ToListAsync();
        }

        //Get loans that are overdue but still active
        public async Task<List<Loan>> GetOverdueActiveLoansAsync()
        {
            return await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .Where(l =>
                    l.Status == LoanStatus.Active &&
                    l.EndDate.Date < DateTime.UtcNow.Date &&
                    l.ActualReturnDate == null)
                .ToListAsync();
        }


        //Paged queries — UI

        public async Task<PagedResult<Loan>> GetByBorrowerIdPagedAsync(
            string borrowerId,
            LoanFilter? filter,
            PagedRequest request)
        {
            var query = _context.Loans
                .AsNoTracking()
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(l => l.Item)
                    .ThenInclude(i => i.Photos)
                .Include(l => l.Lender)
                .Include(l => l.Fines)
                .Include(l => l.Disputes)
                .Where(l => l.BorrowerId == borrowerId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Loan>> GetByOwnerIdPagedAsync(
            string ownerId,
            LoanFilter? filter,
            PagedRequest request)
        {
            var query = _context.Loans
                .AsNoTracking()
                .AsSplitQuery()
                .IgnoreQueryFilters() 
                .Include(l => l.Item)
                    .ThenInclude(i => i.Photos)
                .Include(l => l.Borrower)
                .Include(l => l.Fines)
                .Include(l => l.Disputes)
                .Where(l => l.LenderId == ownerId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<Loan>> GetAllAsAdminAsync(
            LoanFilter? filter,
            PagedRequest request)
        {
            var query = _context.Loans
                .AsNoTracking()
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(l => l.Item)
                    .ThenInclude(i => i.Photos)
                .Include(l => l.Borrower)
                .Include(l => l.Lender)
                .Include(l => l.Fines)
                .Include(l => l.Disputes)
                .Include(l => l.Messages)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //for user profile
        public async Task<int> GetAllCompletedLoansCountByUserIdAsync(string userId)
        {
            return await _context.Loans
                .CountAsync(l =>
                    (l.BorrowerId == userId || l.LenderId == userId) &&
                    l.Status == LoanStatus.Completed
                );
        }



        //Checks
        public async Task<bool> ExistsAsync(int loanId)
        {
            return await _context.Loans
                .AnyAsync(l => l.Id == loanId);
        }
        public async Task<bool> HasOngoingLoansAsBorrower(string userId)
        {
            return await _context.Loans
                .AnyAsync(l => l.BorrowerId == userId &&
                               OngoingStatuses.Contains(l.Status));
        }

        public async Task<bool> HasOngoingLoansAsOwner(string userId)
        {
            return await _context.Loans
                .AnyAsync(l => l.LenderId == userId &&
                               OngoingStatuses.Contains(l.Status));
        }

        public async Task<bool> HasOverlappingLoanAsync(int itemId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            return await _context.Loans
                .AnyAsync(l => l.ItemId == itemId &&
                               l.Status != LoanStatus.Cancelled &&
                               l.Status != LoanStatus.Rejected &&
                               l.StartDate.Date <= end &&
                               l.EndDate.Date >= start);
        }

        public async Task<bool> HasActiveOrApprovedLoansByItemIdAsync(int itemId)
        {
            return await _context.Loans
                .AnyAsync(l => l.ItemId == itemId &&
                               (l.Status == LoanStatus.Active ||
                                l.Status == LoanStatus.Approved));
        }

        public async Task<bool> IsItemAvailableForDatesAsync(int itemId, DateTime startDate, DateTime endDate)
        {
            var hasOverlap = await HasOverlappingLoanAsync(itemId, startDate, endDate);
            return !hasOverlap;
        }


        public async Task<Loan?> GetActiveLoanByItemAndUserAsync(int itemId, string userId)
        {
            var activeStatuses = new[]
            {
                LoanStatus.Pending,
                LoanStatus.AdminPending,
                LoanStatus.Approved,
                LoanStatus.Active,
                LoanStatus.Late
            };

            return await _context.Loans
                .AsNoTracking()
                .Include(l => l.Item).ThenInclude(i => i.Photos)
                .Include(l => l.Lender)
                .Include(l => l.Borrower)
                .Include(l => l.Fines)
                .Include(l => l.SnapshotPhotos)
                .FirstOrDefaultAsync(l =>
                    l.ItemId == itemId &&
                    l.BorrowerId == userId &&
                    activeStatuses.Contains(l.Status));
        }


        public async Task<int> GetCompletedLoansCountAsync()
        {
            return await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Completed);
        }


        //Counts — admin dashboard
        public async Task<int> GetPendingAdminApprovalsCountAsync()
        {
            return await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.AdminPending);
        }

        public async Task<int> GetActiveLoansCountAsync()
        {
            return await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Active);
        }



        //CRUD

        public async Task AddAsync(Loan loan)
        {
            await _context.Loans.AddAsync(loan);
        }
        public void Update(Loan loan)
        {
            _context.Loans.Update(loan);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }


        //Helpers
        private static IQueryable<Loan> ApplyFilter(IQueryable<Loan> query, LoanFilter? filter)
        {
            if (filter is null) return query;

            if (filter.UserId != null)
                query = query.Where(l => l.BorrowerId == filter.UserId || l.LenderId == filter.UserId);

            if (filter.BorrowerId != null)
                query = query.Where(l => l.BorrowerId == filter.BorrowerId);

            if (filter.LenderId != null)
                query = query.Where(l => l.LenderId == filter.LenderId);

            if (filter.ItemId.HasValue)
                query = query.Where(l => l.ItemId == filter.ItemId.Value);

            if (filter.Status.HasValue)
                query = query.Where(l => l.Status == filter.Status.Value);

            if (filter.ExtensionRequestStatus.HasValue)
                query = query.Where(l => l.ExtensionRequestStatus == filter.ExtensionRequestStatus.Value);

            // Overdue = active loan past end date with no return
            if (filter.IsOverdue == true)
                query = query.Where(l => l.Status == LoanStatus.Active &&
                                         l.EndDate.Date < DateTime.UtcNow.Date &&
                                         l.ActualReturnDate == null);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.StartsAfter.HasValue)
                query = query.Where(l => l.StartDate.Date >= filter.StartsAfter.Value.Date);

            if (filter.EndsBefore.HasValue)
                query = query.Where(l => l.EndDate.Date <= filter.EndsBefore.Value.Date);

            if (filter.HasFines == true)
                query = query.Where(l => l.Fines.Any());

            if (filter.HasDisputes == true)
                query = query.Where(l => l.Disputes.Any());

            if (filter.HasMessages == true)
                query = query.Where(l => l.Messages.Any());

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(l =>
                    EF.Functions.Like(l.Item.Title, $"%{term}%") ||
                    EF.Functions.Like(l.Borrower.UserName!, $"%{term}%") ||
                    EF.Functions.Like(l.Lender.UserName!, $"%{term}%"));
            }

            return query;
        }

        private static IQueryable<Loan> ApplySort(IQueryable<Loan> query, PagedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SortBy))
                return query.OrderByDescending(l => l.CreatedAt);

            return request.SortBy.ToLower() switch
            {
                "startdate" => request.SortDescending
                    ? query.OrderByDescending(l => l.StartDate)
                    : query.OrderBy(l => l.StartDate),

                "enddate" => request.SortDescending
                    ? query.OrderByDescending(l => l.EndDate)
                    : query.OrderBy(l => l.EndDate),

                "totalprice" => request.SortDescending
                    ? query.OrderByDescending(l => l.TotalPrice)
                    : query.OrderBy(l => l.TotalPrice),

                "status" => request.SortDescending
                    ? query.OrderByDescending(l => l.Status)
                    : query.OrderBy(l => l.Status),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(l => l.CreatedAt)
                    : query.OrderBy(l => l.CreatedAt),

                "borrowername" => request.SortDescending
                    ? query.OrderByDescending(l => l.Borrower.UserName)
                    : query.OrderBy(l => l.Borrower.UserName),

                "lendername" => request.SortDescending
                    ? query.OrderByDescending(l => l.Lender.UserName)
                    : query.OrderBy(l => l.Lender.UserName),

                "itemtitle" => request.SortDescending
                    ? query.OrderByDescending(l => l.Item.Title)
                    : query.OrderBy(l => l.Item.Title),

                _ => query.OrderByDescending(l => l.CreatedAt)
            };
        }
    }
}