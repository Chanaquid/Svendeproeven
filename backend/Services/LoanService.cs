using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IFineRepository _fineRepository;
        private readonly IScoreHistoryRepository _scoreHistoryRepository;
        private readonly IItemReviewRepository _itemReviewRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoanService(
            ILoanRepository loanRepository,
            IItemRepository itemRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            IFineRepository fineRepository,
            IScoreHistoryRepository scoreHistoryRepository,
            IItemReviewRepository itemReviewRepository,
            UserManager<ApplicationUser> userManager)
        {
            _loanRepository = loanRepository;
            _itemRepository = itemRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _fineRepository = fineRepository;
            _scoreHistoryRepository = scoreHistoryRepository;
            _itemReviewRepository = itemReviewRepository;
            _userManager = userManager;
        }

        //Borrower
        public async Task<LoanDto> CreateLoanAsync(string borrowerId, CreateLoanDto dto)
        {
            var borrower = await _userRepository.GetByIdAsync(borrowerId)
                ?? throw new KeyNotFoundException("Borrower not found.");

            var item = await _itemRepository.GetByIdWithDetailsAsync(dto.ItemId)
                ?? throw new KeyNotFoundException("Item not found.");

            if (item.IsDeleted || !item.IsActive)
                throw new InvalidOperationException("This item is not available.");

            if (item.Status != ItemStatus.Approved)
                throw new InvalidOperationException("This item is not approved for borrowing.");

            if (item.OwnerId == borrowerId)
                throw new InvalidOperationException("You cannot borrow your own item.");

            if (item.RequiresVerification && !borrower.IsVerified)
                throw new InvalidOperationException("This item requires a verified account.");

            var startDate = dto.StartDate.Date;
            var endDate = dto.EndDate.Date;

            if (startDate < DateTime.UtcNow.Date)
                throw new ArgumentException("Start date cannot be in the past.");

            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");

            if (startDate < item.AvailableFrom.Date)
                throw new ArgumentException($"Item is not available until {item.AvailableFrom:yyyy-MM-dd}.");

            if (endDate > item.AvailableUntil.Date)
                throw new ArgumentException($"Item is not available after {item.AvailableUntil:yyyy-MM-dd}.");

            var totalDays = (endDate - startDate).Days + 1;

            if (item.MinLoanDays.HasValue && totalDays < item.MinLoanDays.Value)
                throw new ArgumentException($"Minimum loan period is {item.MinLoanDays} days.");

            if (item.MaxLoanDays.HasValue && totalDays > item.MaxLoanDays.Value)
                throw new ArgumentException($"Maximum loan period is {item.MaxLoanDays} days.");

            if (!await _loanRepository.IsItemAvailableForDatesAsync(dto.ItemId, startDate, endDate))
                throw new InvalidOperationException("Item is already booked for the selected dates.");

            var totalPrice = item.IsFree ? 0 : item.PricePerDay * totalDays;

            var status = borrower.Score switch
            {
                < 20 => throw new InvalidOperationException(
                    "Your score is too low to borrow items. Please appeal to restore your score."),
                < 50 => LoanStatus.AdminPending,
                _ => LoanStatus.Pending
            };

            var loan = new Loan
            {
                ItemId = item.Id,
                LenderId = item.OwnerId,
                BorrowerId = borrowerId,
                StartDate = startDate,
                EndDate = endDate,
                TotalPrice = totalPrice,
                PricePerDaySnapshot = item.PricePerDay,
                SnapshotCondition = item.Condition,
                NoteToOwner = dto.NoteToOwner?.Trim(),
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            await _loanRepository.AddAsync(loan);
            await _loanRepository.SaveChangesAsync();

            if (status == LoanStatus.AdminPending)
            {
                await _notificationService.SendToAdminsAsync(
                    NotificationType.LoanRequested,
                    $"Loan request from '{borrower.FullName}' requires admin approval (low score).",
                    loan.Id,
                    NotificationReferenceType.Loan);
            }
            else
            {
                await _notificationService.SendAsync(
                    item.OwnerId,
                    NotificationType.LoanRequested,
                    $"{borrower.FullName} has requested to borrow '{item.Title}'.",
                    loan.Id,
                    NotificationReferenceType.Loan);
            }

            return await GetByIdAsync(loan.Id, borrowerId);
        }

        public async Task<LoanDto> CancelLoanAsync(string borrowerId, int loanId, string? reason)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != borrowerId)
                throw new UnauthorizedAccessException("You do not have permission to cancel this loan.");

            var cancellableStatuses = new[]
            {
                LoanStatus.Pending,
                LoanStatus.AdminPending,
                LoanStatus.Approved
            };

            if (!cancellableStatuses.Contains(loan.Status))
                throw new InvalidOperationException($"A loan with status '{loan.Status}' cannot be cancelled.");

            loan.Status = LoanStatus.Cancelled;
            loan.DecisionNote = reason;
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.LenderId,
                NotificationType.LoanCancelled,
                $"{loan.Borrower.FullName} cancelled their loan request for '{loan.Item.Title}'.",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, borrowerId);
        }

        public async Task<LoanDto> RequestExtensionAsync(string borrowerId, int loanId, RequestExtensionDto dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != borrowerId)
                throw new UnauthorizedAccessException("You do not have permission to request an extension.");

            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Extended)
                throw new InvalidOperationException("Only active loans can be extended.");

            if (loan.ExtensionRequestStatus == ExtensionStatus.Pending)
                throw new InvalidOperationException("An extension request is already pending.");

            if (loan.ExtensionRequestStatus == ExtensionStatus.Approved)
                throw new InvalidOperationException("This loan has already been extended once.");

            var requestedDate = dto.RequestedExtensionDate.Date;

            if (requestedDate <= loan.EndDate.Date)
                throw new ArgumentException("Extension date must be after the current end date.");

            if (requestedDate > loan.Item.AvailableUntil.Date)
                throw new ArgumentException($"Extension cannot exceed the item's availability ({loan.Item.AvailableUntil:yyyy-MM-dd}).");

            if (!await _loanRepository.IsItemAvailableForDatesAsync(
                loan.ItemId,
                loan.EndDate.AddDays(1),
                requestedDate))
                throw new InvalidOperationException("The item is already booked during the requested extension period.");

            loan.RequestedExtensionDate = requestedDate;
            loan.ExtensionRequestStatus = ExtensionStatus.Pending;
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.LenderId,
                NotificationType.LoanRequested,
                $"{loan.Borrower.FullName} has requested an extension until {requestedDate:yyyy-MM-dd} for '{loan.Item.Title}'.",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, borrowerId);
        }


        //Owner
        public async Task<LoanDto> DecideLoanAsync(string ownerId, int loanId, OwnerDecideLoanDto dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.LenderId != ownerId)
                throw new UnauthorizedAccessException("You do not have permission to decide on this loan.");

            if (loan.Status != LoanStatus.Pending)
                throw new InvalidOperationException("Only pending loans can be approved or rejected by the owner.");

            loan.Status = dto.IsApproved ? LoanStatus.Approved : LoanStatus.Rejected;
            loan.OwnerApproverId = ownerId;
            loan.OwnerApprovedAt = DateTime.UtcNow;
            loan.DecisionNote = dto.DecisionNote;
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                dto.IsApproved ? NotificationType.LoanApproved : NotificationType.LoanRejected,
                dto.IsApproved
                    ? $"Your loan request for '{loan.Item.Title}' was approved."
                    : $"Your loan request for '{loan.Item.Title}' was rejected. {dto.DecisionNote}",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, ownerId);
        }

        public async Task<LoanDto> DecideExtensionAsync(string ownerId, int loanId, DecideExtensionDto dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.LenderId != ownerId)
                throw new UnauthorizedAccessException("You do not have permission to decide on this extension.");

            if (loan.ExtensionRequestStatus != ExtensionStatus.Pending)
                throw new InvalidOperationException("No pending extension request found.");

            if (dto.IsApproved)
            {
                var oldEndDate = loan.EndDate;
                var newEndDate = loan.RequestedExtensionDate!.Value;
                var extraDays = (newEndDate - oldEndDate).Days;

                loan.EndDate = newEndDate;
                loan.ExtensionRequestStatus = ExtensionStatus.Approved;
                loan.Status = LoanStatus.Extended;

                if (!loan.Item.IsFree)
                    loan.TotalPrice += loan.PricePerDaySnapshot * extraDays;
            }
            else
            {
                loan.ExtensionRequestStatus = ExtensionStatus.Rejected;
            }

            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                dto.IsApproved ? NotificationType.LoanApproved : NotificationType.LoanRejected,
                dto.IsApproved
                    ? $"Your extension request for '{loan.Item.Title}' was approved until {loan.EndDate:yyyy-MM-dd}."
                    : $"Your extension request for '{loan.Item.Title}' was rejected. {dto.Note}",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, ownerId);
        }

        //QR Scanning
        public async Task<LoanDto> ConfirmPickupAsync(string scannerId, ScanQrCodeDto dto)
        {
            var item = await _itemRepository.GetByQrCodeAsync(dto.QrCode)
                ?? throw new KeyNotFoundException("Item not found for this QR code.");

            var loan = await _loanRepository.GetActiveLoanByItemIdAsync(item.Id);

            loan ??= await _loanRepository.GetByIdWithDetailsAsync(
                (await _loanRepository.GetByOwnerIdAsync(item.OwnerId))
                .Where(l => l.ItemId == item.Id && l.Status == LoanStatus.Approved)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.Id)
                .FirstOrDefault());

            if (loan == null)
                throw new InvalidOperationException("No approved loan found for this item.");

            if (loan.Status != LoanStatus.Approved)
                throw new InvalidOperationException("This loan is not in an approved state for pickup.");

            //Ensure either the lender or borrower is performing the scan
            if (scannerId != loan.BorrowerId)
                throw new UnauthorizedAccessException("Only the borrower can confirm pickup.");

            var snapshotPhotos = item.Photos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new LoanSnapshotPhoto
                {
                    LoanId = loan.Id,
                    PhotoUrl = p.PhotoUrl,
                    DisplayOrder = p.DisplayOrder,
                    SnapshotTakenAt = DateTime.UtcNow
                }).ToList();

            loan.Status = LoanStatus.Active;
            loan.PickedUpAt = DateTime.UtcNow;
            loan.SnapshotPhotos = snapshotPhotos;
            loan.UpdatedAt = DateTime.UtcNow;

            item.Availability = ItemAvailability.OnRent;
            _itemRepository.Update(item);

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.LenderId,
                NotificationType.LoanActive,
                $"'{item.Title}' has been picked up by {loan.Borrower.FullName}.",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, scannerId);
        }

        public async Task<LoanDto> ConfirmReturnAsync(string scannerId, ScanQrCodeDto dto)
        {
            var item = await _itemRepository.GetByQrCodeAsync(dto.QrCode)
                ?? throw new KeyNotFoundException("Item not found for this QR code.");

            var loan = await _loanRepository.GetActiveLoanByItemIdAsync(item.Id)
                ?? throw new InvalidOperationException("No active loan found for this item.");

            if (scannerId != loan.BorrowerId)
                throw new UnauthorizedAccessException("Only the borrower can confirm return.");

            var returnDate = DateTime.UtcNow;
            var isLate = returnDate.Date > loan.EndDate.Date;

            loan.Status = LoanStatus.Completed;
            loan.ReturnedAt = returnDate;
            loan.ActualReturnDate = returnDate;
            //Set 14-day dispute window from return date
            loan.DisputeDeadline = returnDate.AddDays(14);
            loan.UpdatedAt = DateTime.UtcNow;

            item.Availability = ItemAvailability.Available;
            _itemRepository.Update(item);

            //Score logic
            var borrower = loan.Borrower;

            if (isLate)
            {
                //-5 points per day, capped at -15 per item loan
                var daysLate = (returnDate.Date - loan.EndDate.Date).Days;
                var penalty = -Math.Min(daysLate * 5, 15);
                var newScore = Math.Max(0, borrower.Score + penalty);

                borrower.Score = newScore;
                await _scoreHistoryRepository.AddAsync(new ScoreHistory
                {
                    UserId = loan.BorrowerId,
                    PointsChanged = penalty,
                    ScoreAfterChange = newScore,
                    Reason = ScoreChangeReason.LateReturn,
                    LoanId = loan.Id,
                    Note = $"{daysLate} day(s) late.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (borrower.Score < 100)
            {
                //Skip entirely if score already at 100
                var newScore = Math.Min(100, borrower.Score + 5);
                borrower.Score = newScore;
                await _scoreHistoryRepository.AddAsync(new ScoreHistory
                {
                    UserId = loan.BorrowerId,
                    PointsChanged = 5,
                    ScoreAfterChange = newScore,
                    Reason = ScoreChangeReason.OnTimeReturn,
                    LoanId = loan.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.LenderId,
                NotificationType.LoanReturned,
                $"'{item.Title}' has been returned by {borrower.FullName}{(isLate ? " (late)" : "")}.",
                loan.Id,
                NotificationReferenceType.Loan);

            await _notificationService.SendAsync(
                loan.BorrowerId,
                NotificationType.LoanReturned,
                $"You have successfully returned '{item.Title}'.",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, scannerId);
        }


        //Admin
        public async Task<LoanDto> AdminReviewLoanAsync(string adminId, int loanId, AdminReviewLoanDto dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.Status != LoanStatus.AdminPending)
                throw new InvalidOperationException("Only admin-pending loans can be reviewed.");

            loan.Status = dto.IsApproved ? LoanStatus.Pending : LoanStatus.Rejected;
            loan.AdminReviewerId = adminId;
            loan.AdminReviewedAt = DateTime.UtcNow;
            loan.DecisionNote = dto.AdminNote;
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            if (dto.IsApproved)
            {
                await _notificationService.SendAsync(
                    loan.LenderId,
                    NotificationType.LoanRequested,
                    $"{loan.Borrower.FullName} has requested to borrow '{loan.Item.Title}'.",
                    loan.Id,
                    NotificationReferenceType.Loan);
            }

            await _notificationService.SendAsync(
                loan.BorrowerId,
                dto.IsApproved ? NotificationType.LoanApproved : NotificationType.LoanRejected,
                dto.IsApproved
                    ? $"Your loan request for '{loan.Item.Title}' passed admin review."
                    : $"Your loan request for '{loan.Item.Title}' was rejected by admin. {dto.AdminNote}",
                loan.Id,
                NotificationReferenceType.Loan);

            return await GetByIdAsync(loan.Id, adminId);
        }

        public async Task<LoanDto> AdminGetByIdAsync(int loanId)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            return MapToLoanDto(loan, null);
        }

        public async Task<PagedResult<LoanListDto>> AdminGetAllAsync(LoanFilter? filter, PagedRequest request)
        {
            var result = await _loanRepository.GetAllAsAdminAsync(filter, request);
            return MapToPagedLoanListDto(result, null);
        }

        public async Task<List<AdminPendingLoanDto>> GetPendingAdminApprovalsAsync()
        {
            var loans = await _loanRepository.GetPendingAdminApprovalsAsync();
            var borrowerIds = loans.Select(l => l.BorrowerId).Distinct().ToList();
            var fineTotals = await _fineRepository.GetOutstandingTotalsByUsersAsync(borrowerIds);

            return loans.Select(l => new AdminPendingLoanDto
            {
                Id = l.Id,
                ItemTitle = l.Item.Title,
                ItemPrimaryPhoto = l.Item.Photos
                    .FirstOrDefault(p => p.IsPrimary)?.PhotoUrl
                    ?? l.Item.Photos.FirstOrDefault()?.PhotoUrl,
                OwnerName = l.Lender.FullName ?? "",
                BorrowerName = l.Borrower.FullName ?? "",
                BorrowerEmail = l.Borrower.Email ?? "",
                BorrowerAvatarUrl = l.Borrower.AvatarUrl,
                BorrowerScore = l.Borrower.Score,
                BorrowerUnpaidFines = fineTotals.GetValueOrDefault(l.BorrowerId, 0),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                TotalPrice = l.TotalPrice,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<int> GetPendingAdminApprovalsCountAsync()
        {
            return await _loanRepository.GetPendingAdminApprovalsCountAsync();
        }

        public async Task<int> GetActiveLoansCountAsync()
        {
            return await _loanRepository.GetActiveLoansCountAsync();
        }


        //Queries
        public async Task<LoanDto> GetByIdAsync(int loanId, string currentUserId)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != currentUserId && loan.LenderId != currentUserId)
            {
                var user = await _userManager.FindByIdAsync(currentUserId);
                bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                    throw new UnauthorizedAccessException("You do not have access to this loan.");
            }

  
            bool canReview = false;
            bool hasReviewed = false;

            if (loan.Status == LoanStatus.Completed && currentUserId == loan.BorrowerId)
            {
                hasReviewed = await _itemReviewRepository.GetItemReviewByLoanIdAsync(loanId) != null;
                canReview = !hasReviewed;
            }

            return MapToLoanDto(loan, currentUserId, canReview, hasReviewed);
        }

        public async Task<PagedResult<LoanListDto>> GetMyLoansAsBorrowerAsync(
            string borrowerId,
            LoanFilter? filter,
            PagedRequest request)
        {
            var result = await _loanRepository.GetByBorrowerIdPagedAsync(borrowerId, filter, request);
            return MapToPagedLoanListDto(result, borrowerId);
        }

        public async Task<PagedResult<LoanListDto>> GetMyLoansAsLenderAsync(
            string lenderId,
            LoanFilter? filter,
            PagedRequest request)
        {
            var result = await _loanRepository.GetByOwnerIdPagedAsync(lenderId, filter, request);
            return MapToPagedLoanListDto(result, lenderId);
        }

        public async Task<PagedResult<LoanListDto>> GetAllLoansByUserIdAsync(
            string userId,
            LoanFilter? filter,
            PagedRequest request,
            bool isAdmin = false)
        {
            if (!isAdmin)
                throw new UnauthorizedAccessException("Admin access required.");

            filter ??= new LoanFilter();
            filter.BorrowerId ??= userId;

            var result = await _loanRepository.GetAllAsAdminAsync(filter, request);
            return MapToPagedLoanListDto(result, null);
        }

        //Mappers

        private static LoanDto MapToLoanDto(
            Loan loan,
            string? currentUserId,
            bool canReview = false,
            bool hasReviewed = false)
        {
            var primaryPhoto = loan.Item.Photos.FirstOrDefault(p => p.IsPrimary)
                ?? loan.Item.Photos.FirstOrDefault();

            return new LoanDto
            {
                Id = loan.Id,
                ItemId = loan.ItemId,
                ItemTitle = loan.Item.Title,
                ItemSlug = loan.Item.Slug,
                ItemMainPhotoUrl = primaryPhoto?.PhotoUrl,

                LenderId = loan.LenderId,
                LenderName = loan.Lender.FullName ?? "",
                LenderUserName = loan.Lender.UserName ?? "",
                LenderAvatarUrl = loan.Lender.AvatarUrl,
                LenderScore = loan.Lender.Score,

                BorrowerId = loan.BorrowerId,
                BorrowerName = loan.Borrower.FullName ?? "",
                BorrowerUserName = loan.Borrower.UserName ?? "",
                BorrowerAvatarUrl = loan.Borrower.AvatarUrl,
                BorrowerScore = loan.Borrower.Score,

                StartDate = loan.StartDate,
                EndDate = loan.EndDate,
                ActualReturnDate = loan.ActualReturnDate,

                RequestedExtensionDate = loan.RequestedExtensionDate,
                ExtensionRequestStatus = loan.ExtensionRequestStatus,

                TotalPrice = loan.TotalPrice,
                NoteToOwner = loan.NoteToOwner,

                PickedUpAt = loan.PickedUpAt,
                ReturnedAt = loan.ReturnedAt,

                Status = loan.Status,
                SnapshotCondition = loan.SnapshotCondition,

                AdminReviewerId = loan.AdminReviewerId,
                AdminReviewerName = loan.AdminReviewer?.FullName,
                AdminReviewerUserName = loan.AdminReviewer?.UserName,
                AdminReviewerAvatarUrl = loan.AdminReviewer?.AvatarUrl,
                AdminReviewedAt = loan.AdminReviewedAt,

                OwnerApprovedAt = loan.OwnerApprovedAt,
                DecisionNote = loan.DecisionNote,

                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt,

                IsOverdue = loan.ActualReturnDate == null &&
                            DateTime.UtcNow.Date > loan.EndDate.Date &&
                            loan.Status == LoanStatus.Active,

                CanBeExtended = loan.Status == LoanStatus.Active &&
                                loan.ExtensionRequestStatus != ExtensionStatus.Approved &&
                                loan.ExtensionRequestStatus != ExtensionStatus.Pending,

                IsMine = currentUserId == loan.BorrowerId,
                IsMyItem = currentUserId == loan.LenderId,

                CanReview = canReview,
                HasReviewed = hasReviewed,

                Fines = loan.Fines.Select(f => new FineDto
                {
                    Id = f.Id,
                    Amount = f.Amount,
                    Status = f.Status,
                    Type = f.Type,
                    CreatedAt = f.CreatedAt
                }).ToList(),

                SnapshotPhotos = loan.SnapshotPhotos
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new LoanSnapshotPhotoDto
                    {
                        Id = s.Id,
                        LoanId = s.LoanId,
                        PhotoUrl = s.PhotoUrl,
                        DisplayOrder = s.DisplayOrder,
                        SnapshotTakenAt = s.SnapshotTakenAt
                    }).ToList()
            };
        }

        private static LoanListDto MapToLoanListDto(Loan loan, string? currentUserId)
        {
            var primaryPhoto = loan.Item.Photos.FirstOrDefault(p => p.IsPrimary)
                ?? loan.Item.Photos.FirstOrDefault();

            bool isBorrower = currentUserId == loan.BorrowerId;

            return new LoanListDto
            {
                Id = loan.Id,
                ItemId = loan.ItemId,
                ItemTitle = loan.Item.Title,
                ItemMainPhotoUrl = primaryPhoto?.PhotoUrl,

                OtherPartyId = isBorrower ? loan.LenderId : loan.BorrowerId,
                OtherPartyName = isBorrower
                    ? (loan.Lender.FullName ?? "")
                    : (loan.Borrower.FullName ?? ""),
                OtherPartyUserName = isBorrower
                    ? (loan.Lender.UserName ?? "")
                    : (loan.Borrower.UserName ?? ""),
                OtherPartyAvatarUrl = isBorrower
                    ? loan.Lender.AvatarUrl
                    : loan.Borrower.AvatarUrl,

                StartDate = loan.StartDate,
                EndDate = loan.EndDate,
                ActualReturnDate = loan.ActualReturnDate,
                Status = loan.Status,
                TotalPrice = loan.TotalPrice,
                IsBorrower = isBorrower,
                CreatedAt = loan.CreatedAt
            };
        }

        private static PagedResult<LoanListDto> MapToPagedLoanListDto(
            PagedResult<Loan> result,
            string? currentUserId)
        {
            return new PagedResult<LoanListDto>
            {
                Items = result.Items.Select(l => MapToLoanListDto(l, currentUserId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}