export interface CreateDisputePhotoDto {
  disputeId: number;
  photoUrl: string;
  caption?: string | null;
}

export interface AddDisputePhotoDto {
  photoUrl: string;
  caption?: string | null;
}

export interface CreateMultipleDisputePhotosDto {
  disputeId: number;
  photoUrls: string[];
  caption?: string | null;
}

export interface DisputePhotoDto {
  id: number;
  disputeId: number;
  submittedById: string;
  submittedByName: string;
  submittedByUserName: string;
  submittedByAvatarUrl: string | null;
  photoUrl: string;
  caption: string | null;
  uploadedAt: string;
  isMine: boolean;
}

export interface DisputePhotoListDto {
  id: number;
  photoUrl: string;
  caption: string | null;
  uploadedAt: string;
}

export interface DeleteDisputePhotoDto {
  photoId: number;
  disputeId: number;
}