import { UserListForUsersDto } from './userDTO';

export interface AdminScoreAdjustDto {
  pointsChanged: number;
  note: string;
}

export interface AdminEditUserDto {
  fullName?: string | null;
  username?: string | null;
  email?: string | null;
  newPassword?: string | null;
  address?: string | null;
  gender?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  isVerified?: boolean | null;
  role?: string | null;
  score?: number | null;
  scoreNote?: string | null;
}

export interface AdminDeleteResultDto {
  success: boolean;
  warnings: string[];
}

export interface AdminUserDto {
  id: string;
  fullName: string;
  username: string;
  email: string;
  phoneNumber: string | null;
  gender: string | null;
  bio: string | null;
  address: string;
  latitude: number | null;
  longitude: number | null;
  avatarUrl: string | null;
  age: number;
  role: string;
  score: number;
  isVerified: boolean;
  membershipDate: string;
  unpaidFinesTotal: number;
  isBanned: boolean;
  bannedAt: string | null;
  banReason: string | null;
  banExpiresAt: string | null;
  bannedByAdminId: string | null;
  bannedByAdminName: string | null;
  bannedByAdminAvatarUrl: string | null;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedByAdminId: string | null;
  deletedByAdminName: string | null;
  deletedByAdminAvatarUrl: string | null;
  deletionNote: string | null;
  createdAt: string;
  updatedAt: string | null;
  emailConfirmed: boolean;
  totalOwnedItems: number;
  totalBorrowedLoans: number;
  totalGivenLoans: number;
  totalFines: number;
  totalScoreHistory: number;
  totalAppeals: number;
  totalSupportThreads: number;
  totalItemReviews: number;
  totalReviewsGiven: number;
  totalReviewsReceived: number;
  totalVerificationRequests: number;
  totalBanHistory: number;
  totalInitiatedDisputes: number;
  totalReceivedDisputes: number;
  totalDisputesResolved: number;
  totalAppealsResolved: number;
  totalVerificationRequestsReviewed: number;
  totalSupportThreadsClaimed: number;
}

export interface UserListForAdminsDto extends UserListForUsersDto {
  email: string | null;
  gender: string | null;
  role: string;
  bio: string | null;
  totalItems: number;
  borrowedLoansCount: number;
  givenLoansCount: number;
  generalAddress: string | null;
  totalReviewsReceived: number;
}