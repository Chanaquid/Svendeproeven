export interface SendSupportMessageDto {
  content: string;
}

export interface SupportMessageDto {
  id: number;
  supportThreadId: number;
  senderId: string;
  senderName: string;
  senderFullName: string;
  senderAvatarUrl: string | null;
  isAdminMessage: boolean;
  isMine: boolean;
  content: string;
  isRead: boolean;
  sentAt: string;
}

export interface SupportMessageListDto {
  id: number;
  senderName: string;
  senderUserName: string;
  senderAvatarUrl: string | null;
  isAdmin: boolean;
  content: string;
  isRead: boolean;
  sentAt: string;
  isMine: boolean;
}

export interface MarkSupportMessagesReadDto {
  upToMessageId?: number | null;
}

export interface SupportUnreadCountDto {
  supportThreadId: number;
  unreadCount: number;
}