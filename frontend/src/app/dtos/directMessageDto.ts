export interface SendDirectMessageDto {
  content: string;
}

export interface DirectMessageDto {
  id: number;
  conversationId: number;
  senderId: string;
  senderFullName: string;
  senderUserName: string;
  senderAvatarUrl: string | null;
  content: string;
  isRead: boolean;
  readAt: string | null;
  sentAt: string;
  isMine: boolean;
}