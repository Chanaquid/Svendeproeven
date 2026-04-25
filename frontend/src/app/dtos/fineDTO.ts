import { FineStatus, FineType, PaymentMethod } from './enums';

export interface CreateLoanDisputeFineDto {
  userId: string;
  amount: number;
  loanId: number;
  disputeId: number;
  adminNote?: string | null;
}

export interface CreateCustomFineDto {
  userId: string;
  amount: number;
  reason: string;
}

export interface SubmitPaymentProofDto {
  fineId: number;
  paymentMethod: PaymentMethod;
  paymentDescription: string;
  paymentProofImageUrl: string;
}

export interface UpdateFineDto {
  fineId: number;
  amount?: number | null;
  reason?: string | null;
  status?: FineStatus | null;
}

export interface AdminFineVerifyPaymentDto {
  isApproved: boolean;
  rejectionReason?: string | null;
}

export interface FineDto {
  id: number;
  userId: string;
  userName: string;
  fullName: string;
  userAvatarUrl: string | null;
  isMine: boolean;
  disputeId: number | null;
  loanId: number | null;
  itemTitle: string | null;
  itemSlug: string | null;
  type: FineType;
  status: FineStatus;
  amount: number;
  itemValueAtTimeOfFine: number | null;
  paymentMethod: PaymentMethod | null;
  paymentProofImageUrl: string | null;
  paymentDescription: string | null;
  rejectionReason: string | null;
  issuedByAdminId: string | null;
  issuedByAdminName: string | null;
  issuedByAdminUserName: string | null;
  issuedByAdminAvatarUrl: string | null;
  adminNote: string | null;
  verifiedByAdminId: string | null;
  verifiedByAdminName: string | null;
  verifiedByAdminUserName: string | null;
  verifiedByAdminAvatarUrl: string | null;
  hasPendingAppeal: boolean;
  activeAppealId: number | null;
  proofSubmittedAt: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface FineListDto {
  id: number;
  userId: string;
  fullName: string;
  userName: string;
  userAvatarUrl: string | null;
  loanId: number | null;
  disputeId: number | null;
  itemTitle: string | null;
  type: FineType;
  status: FineStatus;
  amount: number;
  hasPendingAppeal: boolean;
  createdAt: string;
  paidAt: string | null;
  issuedByAdminId: string;
  issuedByAdminName: string;
  issuedByAdminUsername: string;
  issuedByAdminUserAvatarUrl: string | null;
}

export interface FineStatsDto {
  totalUnpaid: number;
  pendingProofReview: number;
  totalOutstandingAmount: number;
  issuedThisMonth: number;
  statusBreakdown: Record<FineStatus, number>;
  typeBreakdown: Record<FineType, number>;
}