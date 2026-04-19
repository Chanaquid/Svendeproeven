export interface BanUserDto {
  reason: string;
  banExpiresAt?: string | null; // null = permanent
}

export interface UnbanUserDto {
  reason: string;
  note?: string | null;
}

export interface UserBanHistoryDto {
  id: number;
  bannedUserId: string;
  bannedFullName: string;
  bannedUserName: string;
  bannedUserAvatarUrl: string | null;
  adminId: string;
  adminFullName: string;
  adminUserName: string;
  adminAvatarUrl: string | null;
  isBanned: boolean;
  reason: string;
  note: string | null;
  bannedAt: string;
  banExpiresAt: string | null;
}

export interface UserBanHistoryListDto {
  id: number;
  adminName: string;
  adminUserName: string;
  isBanned: boolean;
  reason: string;
  bannedAt: string;
  banExpiresAt: string | null;
}