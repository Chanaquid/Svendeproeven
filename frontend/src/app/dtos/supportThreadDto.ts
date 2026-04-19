import { SupportThreadStatus } from './enums';
import { SupportMessageDto } from './supportMessageDto';

export interface CreateSupportThreadDto {
  subject: string;
  initialMessage: string;
}

export interface UpdateSupportThreadStatusDto {
  status: SupportThreadStatus;
}

export interface ClaimSupportThreadDto {
  threadId: number;
}

export interface SupportThreadDto {
  id: number;
  subject: string;
  userId: string;
  fullName: string;
  userName: string;
  userAvatarUrl: string | null;
  claimedByAdminId: string | null;
  claimedByAdminName: string | null;
  claimedByAdminUserName: string | null;
  claimedByAdminAvatarUrl: string | null;
  status: SupportThreadStatus;
  claimedAt: string | null;
  closedAt: string | null;
  createdAt: string;
  messages: SupportMessageDto[];
}

export interface SupportThreadListDto {
  id: number;
  subject: string;
  fullName: string;
  userName: string;
  userAvatarUrl: string | null;
  claimedByAdminName: string | null;
  status: SupportThreadStatus;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
  createdAt: string;
}