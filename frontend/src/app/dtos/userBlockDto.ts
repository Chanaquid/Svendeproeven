
export interface UserBlockDto {
  blockerId: string;
  blockerName: string;
  blockerUserName: string;
  blockerAvatarUrl: string | null;
  blockedId: string;
  blockedName: string;
  blockedUserName: string;
  blockedAvatarUrl: string | null;
  createdAt: string;
}

export interface UserBlockListDto {
  blockedId: string;
  blockedName: string;
  blockedUserName: string;
  blockedAvatarUrl: string | null;
  createdAt: string;
}