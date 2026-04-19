import { DirectMessageDto } from './directMessageDto';

export interface CreateDirectConversationDto {
  initialMessage: string;
}

export interface DirectConversationListDto {
  id: number;
  otherUserId: string;
  otherUserFullName: string;
  otherUserName: string;
  otherUserAvatarUrl: string | null;
  lastMessageContent: string | null;
  lastMessageSentAt: string | null;
  lastMessageSenderId: string | null;
  lastMessageSenderName: string | null;
  lastMessageAvatarUrl: string | null;
  isBlocked: boolean;
  unreadCount: number;
  createdAt: string;
  isInitiatedByMe: boolean;
}

export interface DirectConversationDto {
  id: number;
  initiatedById: string;
  initiatedByFullName: string;
  initiatedByUserName: string;
  initiatedByAvatarUrl: string | null;
  otherUserId: string;
  otherUserFullName: string;
  otherUserName: string;
  otherUserAvatarUrl: string | null;
  hiddenForInitiator: boolean;
  hiddenForOther: boolean;
  isBlocked: boolean;
  initiatorDeletedAt: string | null;
  otherDeletedAt: string | null;
  createdAt: string;
  lastMessageAt: string | null;
  isHiddenForCurrentUser: boolean;
  deletedAtForCurrentUser: string | null;
  canSendMessage: boolean;
  unreadCount: number;
  messages: DirectMessageDto[];
}

export interface UnreadCountsDto {
  conversationUnreadCounts: Record<number, number>;
  totalUnreadCount: number;
}