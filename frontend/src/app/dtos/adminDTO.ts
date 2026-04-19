export interface AdminDashboardDto {
  pendingItemApprovals: number;
  pendingLoanApprovals: number;
  openDisputes: number;
  pendingAppeals: number;
  pendingUserVerifications: number;
  pendingPaymentVerifications: number;
  pendingReports: number;
  totalUsers: number;
  totalActiveItems: number;
  totalActiveLoans: number;
  totalUnpaidFines: number;
  totalUnpaidFinesAmount: number;
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