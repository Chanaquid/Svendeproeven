import { VerificationDocumentType, VerificationStatus } from './enums';

export interface CreateVerificationRequestDto {
  documentType: VerificationDocumentType;
  documentUrl: string;
}

export interface AdminDecideVerificationRequestDto {
  status: VerificationStatus;
  adminNote?: string | null;
}

export interface VerificationRequestDto {
  id: number;
  userId: string;
  fullName: string;
  userName: string;
  userAvatarUrl: string | null;
  documentType: VerificationDocumentType;
  documentUrl: string;
  status: VerificationStatus;
  adminNote: string | null;
  reviewedByAdminId: string | null;
  reviewedByAdminName: string | null;
  reviewedByAdminAvatarUrl: string | null;
  submittedAt: string;
  reviewedAt: string | null;
}

export interface VerificationRequestListDto {
  id: number;
  fullName: string;
  userName: string;
  userAvatarUrl: string | null;
  documentType: VerificationDocumentType;
  status: VerificationStatus;
  reviewedByAdminName: string | null;
  submittedAt: string;
  reviewedAt: string | null;
}