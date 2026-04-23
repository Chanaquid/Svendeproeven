import { DisputeFiledAs, DisputeStatus, DisputeVerdict, ItemCondition } from './enums';
import { DisputePhotoDto } from './disputePhotoDto';
import { LoanSnapshotPhotoDto } from './loanSnapshotPhotoDto';

export interface CreateDisputeDto {
  loanId: number;
  filedAs: DisputeFiledAs;
  description: string;
}

export interface SubmitDisputeResponseDto {
  responseDescription: string;
}

export interface EditDisputeDto {
  description: string;
}

export interface DisputePenaltyDto {
  fineAmount?: number | null;
  scoreAdjustment?: number | null;
}

export interface AdminResolveDisputeDto {
  verdict: DisputeVerdict;
  adminNote?: string | null;
  ownerPenalty?: DisputePenaltyDto | null;
  borrowerPenalty?: DisputePenaltyDto | null;
}

export interface DisputePenaltySummaryDto {
  userId: string;
  fullName: string;
  userName: string;
  avatarUrl: string | null;
  fineAmount: number | null;
  fineStatus: string | null;
  scoreAdjustment: number | null;
}

export interface DisputeDto {
  id: number;
  loanId: number;
  itemId: number;
  itemTitle: string;
  isMine: boolean;
  filedById: string;
  filedByName: string;
  filedByUserName: string;
  filedByAvatarUrl: string | null;
  filedAs: DisputeFiledAs;
  description: string;
  filedByPhotos: DisputePhotoDto[];
  respondedById: string | null;
  respondedByName: string | null;
  respondedByUserName: string | null;
  respondedByAvatarUrl: string | null;
  responseDescription: string | null;
  respondedAt: string | null;
  responseDeadline: string;
  responsePhotos: DisputePhotoDto[];
  status: DisputeStatus;
  createdAt: string;
  resolvedAt: string | null;
  adminVerdict: DisputeVerdict | null;
  adminNote: string | null;
  resolvedByAdminId: string | null;
  resolvedByAdminName: string | null;
  resolvedByAdminUserName: string | null;
  resolvedByAdminAvatarUrl: string | null;
  penalties: DisputePenaltySummaryDto[];
  snapshotCondition: ItemCondition | null;
  snapshotPhotos: LoanSnapshotPhotoDto[];
 
  canEdit: boolean;
  canRespond: boolean;
  canAddEvidence: boolean;
  canCancel: boolean;
  canAddResponseEvidence: boolean;
}


export interface DisputeListDto {
  id: number;
  loanId: number;
  itemTitle: string;
  filedById: string;
  filedByName: string;
  filedByUsername: string;
  filedByAvatarUrl: string | null;
  filedAs: DisputeFiledAs;
  otherPartyName: string | null;
  otherPartyUserName: string | null;
  otherPartyAvatarUrl: string | null;
  status: DisputeStatus;
  hasResponse: boolean;
  isOverDue: boolean;
  responseDeadline: string;
  createdAt: string;
}

export interface DisputeStatsDto {
  totalOpen: number;
  awaitingResponse: number;
  underReview: number;
  overdueResponse: number;
  resolvedThisMonth: number;
  verdictBreakdown: Record<DisputeVerdict, number>;
}