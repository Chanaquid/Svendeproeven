import { ReportReason, ReportStatus, ReportType } from './enums';

export interface CreateReportDto {
  type: ReportType;
  targetId: string;
  reasons: ReportReason;
  additionalDetails?: string | null;
}

export interface AdminResolveReportDto {
  status: ReportStatus;
  adminNote?: string | null;
}

export interface AdminUpdateReportStatusDto {
  status: ReportStatus;
  adminNote?: string | null;
}

export interface ReportDto {
  id: number;
  reportedById: string;
  reportedByName: string;
  reportedByUserName: string;
  reportedByAvatarUrl: string | null;
  isMine: boolean;
  type: ReportType;
  targetId: string;
  reasons: ReportReason;
  additionalDetails: string | null;
  status: ReportStatus;
  handledByAdminId: string | null;
  handledByAdminName: string | null;
  handledByAdminUserName: string | null;
  handledByAdminAvatarUrl: string | null;
  adminNote: string | null;
  resolvedAt: string | null;
  createdAt: string;
}

export interface ReportListDto {
  id: number;
  reportedByName: string;
  reportedByUserName: string;
  type: ReportType;
  targetId: string;
  reasons: ReportReason;
  status: ReportStatus;
  createdAt: string;
}