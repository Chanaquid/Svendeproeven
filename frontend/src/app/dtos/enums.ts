export enum BorrowingStatus {
  Free = 'Free',
  AdminApproval = 'AdminApproval',
  Blocked = 'Blocked',
}

export enum ItemCondition {
  Excellent = 'Excellent',
  Good = 'Good',
  Fair = 'Fair',
  Poor = 'Poor',
}

export enum ItemStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Deleted = 'Deleted',
}

export enum ItemAvailability {
  Available = 'Available',
  OnRent = 'OnRent',
  Unavailable = 'Unavailable',
}

export enum LoanStatus {
  Pending = 'Pending',
  AdminPending = 'AdminPending',
  Approved = 'Approved',
  Active = 'Active',
  Completed = 'Completed',
  Late = 'Late',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
  Extended = 'Extended',
}

export enum FineType {
  ResultedByDispute = 'ResultedByDispute',
  Custom = 'Custom',
}

export enum FineStatus {
  Unpaid = 'Unpaid',
  PendingVerification = 'PendingVerification',
  Paid = 'Paid',
  Rejected = 'Rejected',
  Voided = 'Voided',
}

export enum ScoreChangeReason {
  OnTimeReturn = 'OnTimeReturn',
  LateReturn = 'LateReturn',
  AdminAdjustment = 'AdminAdjustment',
  ItemDamaged = 'ItemDamaged',
  LoanCancelled = 'LoanCancelled',
  VerificationBonus = 'VerificationBonus',
  AppealOutcome = 'AppealOutcome',
  DisputePenalty = 'DisputePenalty',
}

export enum DisputeFiledAs {
  AsOwner = 'AsOwner',
  AsBorrower = 'AsBorrower',
}

export enum DisputeVerdict {
  OwnerPenalized = 'OwnerPenalized',
  BorrowerPenalized = 'BorrowerPenalized',
  BothPenalized = 'BothPenalized',
  NoPenalty = 'NoPenalty',
}

export enum DisputeStatus {
  AwaitingResponse = 'AwaitingResponse',
  PendingAdminReview = 'PendingAdminReview',
  Resolved = 'Resolved',
  PastDeadline = 'PastDeadline',
  Cancelled = 'Cancelled',
}

export enum NotificationType {
  LoanRequested = 'LoanRequested',
  LoanApproved = 'LoanApproved',
  LoanRejected = 'LoanRejected',
  LoanCancelled = 'LoanCancelled',
  LoanActive = 'LoanActive',
  DueSoon = 'DueSoon',
  LoanOverdue = 'LoanOverdue',
  ItemApproved = 'ItemApproved',
  ItemPendingReview = 'ItemPendingReview',
  ItemRejected = 'ItemRejected',
  FineIssued = 'FineIssued',
  FinePaymentPendingVerification = 'FinePaymentPendingVerification',
  FinePaid = 'FinePaid',
  FineRejected = 'FineRejected',
  FineVoided = 'FineVoided',
  ScoreChanged = 'ScoreChanged',
  DisputeFiled = 'DisputeFiled',
  DisputeResponseSubmitted = 'DisputeResponseSubmitted',
  DisputeResolved = 'DisputeResolved',
  AppealSubmitted = 'AppealSubmitted',
  AppealApproved = 'AppealApproved',
  AppealRejected = 'AppealRejected',
  VerificationApproved = 'VerificationApproved',
  VerificationRejected = 'VerificationRejected',
  LoanMessageReceived = 'LoanMessageReceived',
  DirectMessageReceived = 'DirectMessageReceived',
  SupportMessageReceived = 'SupportMessageReceived',
  LoanReturned = 'LoanReturned',
  ItemAvailable = 'ItemAvailable',
  ReportSubmitted = 'ReportSubmitted',
  ReportResolved = 'ReportResolved',
  ItemDeleted = 'ItemDeleted',
  VerificationSubmitted = 'VerificationSubmitted',
  SupportThreadCreated = 'SupportThreadCreated',
  SupportThreadClosed = 'SupportThreadClosed',
  SupportThreadClaimed = 'SupportThreadClaimed',
  DisputeExpired = 'DisputeExpired',
}

export enum NotificationReferenceType {
  Loan = 'Loan',
  Item = 'Item',
  Dispute = 'Dispute',
  Appeal = 'Appeal',
  Fine = 'Fine',
  Verification = 'Verification',
  LoanConversation = 'LoanConversation',
  DirectConversation = 'DirectConversation',
  SupportThread = 'SupportThread',
  Report = 'Report',
  Review = 'Review',
  ItemFavorites = 'ItemFavorites',
}

export enum MessageType {
  LoanMessage = 'LoanMessage',
  DirectMessage = 'DirectMessage',
  SupportMessage = 'SupportMessage',
}

export enum AppealStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
  Deleted = 'Deleted',
}

export enum AppealType {
  Score = 'Score',
  Fine = 'Fine',
}

export enum FineAppealResolution {
  Voided = 'Voided',
  Custom = 'Custom',
}

export enum VerificationStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

export enum VerificationDocumentType {
  Passport = 'Passport',
  NationalId = 'NationalId',
  DrivingLicense = 'DrivingLicense',
}

export enum ExtensionStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

export enum SupportThreadStatus {
  Open = 'Open',
  Claimed = 'Claimed',
  Closed = 'Closed',
}

export enum PaymentMethod {
  MobilePay = 'MobilePay',
  Card = 'Card',
  BankTransfer = 'BankTransfer',
  Cash = 'Cash',
}

export enum ReportType {
  User = 'User',
  Item = 'Item',
  Review = 'Review',
  Message = 'Message',
}

export enum ReportReason {
  FakeIdentity = 'FakeIdentity',
  Scammer = 'Scammer',
  Harassment = 'Harassment',
  InappropriateContent = 'InappropriateContent',
  FakeListing = 'FakeListing',
  ProhibitedItem = 'ProhibitedItem',
  MisleadingDescription = 'MisleadingDescription',
  Spam = 'Spam',
  Other = 'Other',
}

export enum ReportStatus {
  Pending = 'Pending',
  UnderReview = 'UnderReview',
  Resolved = 'Resolved',
  Dismissed = 'Dismissed',
}