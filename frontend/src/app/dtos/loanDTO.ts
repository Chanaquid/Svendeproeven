import { ExtensionStatus, ItemCondition, LoanStatus } from './enums';
import { DisputeDto } from './disputeDTO';
import { FineDto } from './fineDTO';
import { LoanMessageDto } from './loanMessageDto';
import { LoanSnapshotPhotoDto } from './loanSnapshotPhotoDto';

export interface CreateLoanDto {
  itemId: number;
  noteToOwner?: string | null;
  startDate: string;
  endDate: string;
}

export interface UpdateLoanDatesDto {
  loanId: number;
  startDate: string;
  endDate: string;
}

export interface OwnerDecideLoanDto {
  loanId: number;
  isApproved: boolean;
  decisionNote?: string | null;
}

export interface AdminReviewLoanDto {
  loanId: number;
  isApproved: boolean;
  adminNote?: string | null;
}

export interface RequestExtensionDto {
  loanId: number;
  requestedExtensionDate: string;
}

export interface DecideExtensionDto {
  loanId: number;
  isApproved: boolean;
  note?: string | null;
}

export interface ScanQrCodeDto {
  qrCode: string;
}

export interface CancelLoanDto {
  loanId: number;
  reason?: string | null;
}

export interface LoanDto {
  id: number;
  itemId: number;
  itemTitle: string;
  itemSlug: string;
  itemMainPhotoUrl: string | null;
  lenderId: string;
  lenderName: string;
  lenderUserName: string;
  lenderAvatarUrl: string | null;
  lenderScore: number;
  borrowerId: string;
  borrowerName: string;
  borrowerUserName: string;
  borrowerAvatarUrl: string | null;
  borrowerScore: number;
  noteToOwner: string | null;
  startDate: string;
  endDate: string;
  actualReturnDate: string | null;
  requestedExtensionDate: string | null;
  extensionRequestStatus: ExtensionStatus | null;
  totalPrice: number;
  pickedUpAt: string | null;
  returnedAt: string | null;
  status: LoanStatus;
  snapshotCondition: ItemCondition;
  adminReviewerId: string | null;
  adminReviewerName: string | null;
  adminReviewerUserName: string | null;
  adminReviewerAvatarUrl: string | null;
  adminReviewedAt: string | null;
  ownerApprovedAt: string | null;
  decisionNote: string | null;
  createdAt: string;
  updatedAt: string | null;
  isOverdue: boolean;
  canBeExtended: boolean;
  isMine: boolean;
  isMyItem: boolean;
  canReview: boolean;
  hasReviewed: boolean;
  disputes: DisputeDto[];
  messages: LoanMessageDto[];
  fines: FineDto[];
  snapshotPhotos: LoanSnapshotPhotoDto[];
}

export interface LoanListDto {
  id: number;
  itemId: number;
  itemTitle: string;
  itemMainPhotoUrl: string | null;
  otherPartyId: string;
  otherPartyName: string;
  otherPartyUserName: string;
  otherPartyAvatarUrl: string | null;
  startDate: string;
  endDate: string;
  actualReturnDate: string | null;
  status: LoanStatus;
  totalPrice: number;
  isBorrower: boolean;
  createdAt: string;
}

export interface AdminPendingLoanDto {
  id: number;
  itemTitle: string;
  itemPrimaryPhoto: string | null;
  ownerName: string;
  ownerUserName: string;
  borrowerName: string;
  borrowerUserName: string;
  borrowerEmail: string;
  borrowerAvatarUrl: string | null;
  borrowerScore: number;
  borrowerUnpaidFines: number;
  startDate: string;
  endDate: string;
  totalPrice: number;
  createdAt: string;
}