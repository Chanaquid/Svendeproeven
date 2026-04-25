export interface AdminDashboardDto {
  // Action queues
  pendingItemApprovals: number;
  pendingLoanApprovals: number;
  openDisputes: number;
  overdueDisputeResponses: number;
  pendingAppeals: number;
  pendingUserVerifications: number;
  pendingPaymentVerifications: number;
  pendingReports: number;

  // Users
  totalUsers: number;
  verifiedUsers: number;
  bannedUsers: number;
  newUsersThisWeek: number;

  // Items
  totalActiveItems: number;
  itemsListedThisWeek: number;

  // Loans
  totalActiveLoans: number;
  overdueLoans: number;
  loansCreatedThisWeek: number;

  // Fines
  totalUnpaidFines: number;
  totalUnpaidFinesAmount: number;
  finesCollectedThisMonth: number;
  finesIssuedThisMonth: number;

  // Disputes
  disputesResolvedThisMonth: number;
  averageDisputeResolutionDays: number;
}

export interface ItemReviewEntryDto {
  id: number;
  reviewerId: string;
  reviewerAvatarUrl: string | null;
  reviewerName: string;
  rating: number;
  comment: string | null;
  isAdminReview: boolean;
  createdAt: string;
}

export interface LoanHistoryEntryDto {
  loanId: number;
  borrowerName: string;
  startDate: string;
  endDate: string;
  actualReturnDate: string | null;
  status: string;
  snapshotCondition: string;
}

export interface ItemHistoryDto {
  itemId: number;
  itemTitle: string;
  ownerName: string;
  averageRating: number;
  reviewCount: number;
  reviews: ItemReviewEntryDto[];
  loans: LoanHistoryEntryDto[];
}

export interface AdminBanRequestDto {
  reason: string;
  durationMinutes?: number | null;
}

export interface AdminUnBanRequestDto {
  note?: string | null;
}

export interface BanHistoryResponseDto {
  id: number;
  userId: string;
  userName: string;
  userAvatarUrl: string | null;
  adminId: string;
  adminName: string;
  adminAvatarUrl: string | null;
  isBanned: boolean;
  reason: string;
  note: string | null;
  bannedAt: string;
  banExpiresAt: string | null;
}