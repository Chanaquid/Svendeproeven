import {
  AppealStatus,
  AppealType,
  DisputeFiledAs,
  DisputeStatus,
  DisputeVerdict,
  ExtensionStatus,
  FineStatus,
  FineType,
  ItemAvailability,
  ItemCondition,
  ItemStatus,
  LoanStatus,
  NotificationReferenceType,
  NotificationType,
  ReportReason,
  ReportStatus,
  ReportType,
  ScoreChangeReason,
  SupportThreadStatus,
  VerificationDocumentType,
  VerificationStatus,
} from './enums';

export interface UserFilter {
  includeDeleted?: boolean | null;
  isPermanentBan?: boolean | null;
  isBanned?: boolean | null;
  isDeleted?: boolean | null;
  isVerified?: boolean | null;
  role?: string | null;
  minScore?: number | null;
  maxScore?: number | null;
  hasUnpaidFines?: boolean | null;
  latitude?: number | null;
  longitude?: number | null;
  radiusKm?: number | null;
  search?: string | null;
}

export interface ItemFilter {
  isFree?: boolean | null;
  requiresVerification?: boolean | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  radiusKm?: number | null;
  search?: string | null;
  condition?: ItemCondition | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  categoryId?: number | null;
  ownerId?: string | null;
  maxLoanDays?: number | null;
  minLoanDays?: number | null;
  status?: ItemStatus | null;
  isActive?: boolean | null;
  availability?: ItemAvailability | null;
  minRating?: number | null;
  maxRating?: number | null;
}

export interface ItemReviewFilter {
  minRating?: number | null;
  maxRating?: number | null;
  isVerifiedReviewer?: boolean | null;
  search?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
  hasComment?: boolean | null;
}

export interface LoanFilter {
  borrowerId?: string | null;
  lenderId?: string | null;
  itemId?: number | null;
  status?: LoanStatus | null;
  extensionRequestStatus?: ExtensionStatus | null;
  isOverdue?: boolean | null;
  createdAfter?: string | null;
  startsAfter?: string | null;
  endsBefore?: string | null;
  hasFines?: boolean | null;
  hasDisputes?: boolean | null;
  hasMessages?: boolean | null;
  search?: string | null;
}

export interface NotificationFilter {
  type?: NotificationType | null;
  referenceType?: NotificationReferenceType | null;
  isRead?: boolean | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  referenceId?: number | null;
  search?: string | null;
}

export interface AppealFilter {
  userId?: string | null;
  resolvedByAdminId?: string | null;
  appealType?: AppealType | null;
  status?: AppealStatus | null;
  fineId?: number | null;
  scoreHistoryId?: number | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  resolvedAfter?: string | null;
  resolvedBefore?: string | null;
  isResolved?: boolean | null;
  search?: string | null;
}

export interface DisputeFilter {
  filedById?: string | null;
  respondedById?: string | null;
  resolvedByAdminId?: string | null;
  loanId?: number | null;
  filedAs?: DisputeFiledAs | null;
  status?: DisputeStatus | null;
  adminVerdict?: DisputeVerdict | null;
  isResolved?: boolean | null;
  hasResponse?: boolean | null;
  isOverdueResponse?: boolean | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  resolvedAfter?: string | null;
  resolvedBefore?: string | null;
  responseDeadlineBefore?: string | null;
  responseDeadlineAfter?: string | null;
  minCustomFine?: number | null;
  maxCustomFine?: number | null;
  search?: string | null;
}

export interface FineFilter {
  userId?: string | null;
  issuedByAdminId?: string | null;
  status?: FineStatus | null;
  type?: FineType | null;
  minAmount?: number | null;
  maxAmount?: number | null;
  createdAfter?: string | null;
  paidAfter?: string | null;
  loanId?: number | null;
  disputeId?: number | null;
  hasPaymentProof?: boolean | null;
  search?: string | null;
}

export interface ReportFilter {
  reportedById?: string | null;
  handledByAdminId?: string | null;
  type?: ReportType | null;
  reasons?: ReportReason | null;
  status?: ReportStatus | null;
  targetId?: string | null;
  isResolved?: boolean | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  resolvedAfter?: string | null;
  resolvedBefore?: string | null;
  search?: string | null;
}

export interface VerificationRequestFilter {
  userId?: string | null;
  reviewedByAdminId?: string | null;
  status?: VerificationStatus | null;
  documentType?: VerificationDocumentType | null;
  isReviewed?: boolean | null;
  isApproved?: boolean | null;
  isRejected?: boolean | null;
  submittedAfter?: string | null;
  submittedBefore?: string | null;
  reviewedAfter?: string | null;
  reviewedBefore?: string | null;
  search?: string | null;
}

export interface ScoreHistoryFilter {
  userId?: string | null;
  reason?: ScoreChangeReason | null;
  loanId?: number | null;
  disputeId?: number | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  onlyPositive?: boolean | null;
  onlyNegative?: boolean | null;
  search?: string | null;
}

export interface SupportThreadFilter {
  userId?: string | null;
  claimedByAdminId?: string | null;
  status?: SupportThreadStatus | null;
  isClaimed?: boolean | null;
  isUnclaimed?: boolean | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  claimedAfter?: string | null;
  claimedBefore?: string | null;
  closedAfter?: string | null;
  closedBefore?: string | null;
  search?: string | null;
}

export interface MessageFilter {
  search?: string | null;
  sentAfter?: string | null;
  sentBefore?: string | null;
  isRead?: boolean | null;
  senderId?: string | null;
  includeDeleted?: boolean | null;
}

export interface ConversationFilter {
  search?: string | null;
  lastMessageAfter?: string | null;
  lastMessageBefore?: string | null;
  hasUnreadMessages?: boolean | null;
  includeHidden?: boolean | null;
}

export interface UserBanHistoryFilter {
  userId?: string | null;
  adminId?: string | null;
  isBanned?: boolean | null;
  isPermanent?: boolean | null;
  bannedAfter?: string | null;
  bannedBefore?: string | null;
  search?: string | null;
}

export interface UserBlockFilter {
  blockerId?: string | null;
  blockedId?: string | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  search?: string | null;
}

export interface UserReviewFilter {
  reviewerId?: string | null;
  reviewedUserId?: string | null;
  loanId?: number | null;
  minRating?: number | null;
  maxRating?: number | null;
  isAdminReview?: boolean | null;
  isEdited?: boolean | null;
  createdAfter?: string | null;
  createdBefore?: string | null;
  search?: string | null;
}

export interface UserFavoriteItemFilter {
  itemId?: number | null;
  notifyWhenAvailable?: boolean | null;
  savedAfter?: string | null;
  savedBefore?: string | null;
  onlyAvailable?: boolean | null;
  search?: string | null;
}