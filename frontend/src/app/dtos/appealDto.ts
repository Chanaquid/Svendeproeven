import { AppealStatus, AppealType, FineAppealResolution } from './enums';

export interface CreateScoreAppealDto {
  message: string;
}

export interface CreateFineAppealDto {
  fineId: number;
  message: string;
}

export interface AdminDecidesScoreAppealDto {
  isApproved: boolean;
  adminNote?: string | null;
  newScore?: number | null;
}

export interface AdminDecidesFineAppealDto {
  isApproved: boolean;
  adminNote?: string | null;
  resolution?: FineAppealResolution | null;
  customFineAmount?: number | null;
}

export interface AppealDto {
  id: number;
  userId: string;
  userName: string;
  fullName: string;
  userAvatarUrl: string | null;
  appealType: AppealType;
  message: string;
  status: AppealStatus;
  fineId: number | null;
  fineAmount: number | null;
  fineResolution: FineAppealResolution | null;
  customFineAmount: number | null;
  scoreHistoryId: number | null;
  restoredScore: number | null;
  scoreAfterChange: number | null;
  resolvedByAdminId: string | null;
  resolvedByAdminName: string | null;
  resolvedByAdminUserName: string | null;
  resolvedByAdminAvatarUrl: string | null;
  adminNote: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface AppealListDto {
  id: number;
  userId: string;
  userName: string;
  fullName: string;
  userAvatarUrl: string | null;
  appealType: AppealType;
  status: AppealStatus;
  message: string;
  createdAt: string;
  resolvedAt: string | null;
}

export interface AdminAppealDto extends AppealDto {
  userEmail: string;
  isVerified: boolean;
  userCurrentScore: number;
  unpaidFinesTotal: number;
  membershipDate: string;
  successfulBorrowCount: number;
  successfulLendCount: number;
  hoursToResolve: number | null;
}