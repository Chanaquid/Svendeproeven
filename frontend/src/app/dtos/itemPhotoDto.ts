export interface ItemPhotoDto {
  id: number;
  photoUrl: string;
  isPrimary: boolean;
  displayOrder: number;
  uploadedAt: string;
}

export interface AddItemPhotoDto {
  photoUrl: string;
  isPrimary?: boolean;
  displayOrder?: number;
}

export interface UpdateItemPhotoDto {
  id: number;
  isPrimary?: boolean | null;
  displayOrder?: number | null;
}

export interface ReorderItemPhotosDto {
  itemId: number;
  photoIdsInOrder: number[];
}

export interface SetPrimaryPhotoDto {
  photoId: number;
  itemId: number;
}

export interface PhotoOrderUpdateDto {
  photoId: number;
  displayOrder: number;
}

export interface DeleteItemPhotoDto {
  photoId: number;
  itemId: number;
}