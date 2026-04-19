
import { NotificationReferenceType, NotificationType } from './enums';

export interface NotificationDto {
  id: number;
  type: NotificationType;
  message: string;
  referenceId: number | null;
  referenceType: NotificationReferenceType | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationSummaryDto {
  unreadCount: number;
  recent: NotificationDto[];
}

export interface AdminNotificationDto {
  id: number;
  userId: string;
  userName: string;
  userAvatarUrl: string | null;
  type: NotificationType;
  message: string;
  referenceId: number | null;
  referenceType: NotificationReferenceType | null;
  isRead: boolean;
  createdAt: string;
}

export interface MarkMultipleNotificationsReadDto {
  notificationIds: number[];
}

export interface UnreadNotificationCountDto {
  unreadCount: number;
}