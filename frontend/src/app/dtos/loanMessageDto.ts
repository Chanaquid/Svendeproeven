export interface SendLoanMessageDto {
  content: string;
}

export interface LoanMessageDto {
  id: number;
  loanId: number;
  senderId: string;
  senderName: string;
  senderAvatarUrl: string | null;
  isMine: boolean;
  content: string;
  isRead: boolean;
  readAt: string | null;
  sentAt: string;
}

export interface MarkLoanMessagesReadDto {
  upToMessageId?: number | null;
}

export interface LoanUnreadCountDto {
  loanId: number;
  unreadCount: number;
}