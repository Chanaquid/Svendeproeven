import { ItemAvailability, ItemCondition, ItemStatus } from './enums';
import { ItemPhotoDto } from './itemPhotoDto';

export interface CreateItemDto {
  categoryId: number;
  title: string;
  description: string;
  currentValue: number;
  pricePerDay: number;
  isFree: boolean;
  condition: ItemCondition;
  minLoanDays?: number | null;
  maxLoanDays?: number | null;
  requiresVerification: boolean;
  pickupAddress: string;
  pickupLatitude: number;
  pickupLongitude: number;
  availableFrom: string;
  availableUntil: string;
}

export interface UpdateItemDto {
  title?: string | null;
  description?: string | null;
  categoryId?: number | null;
  currentValue?: number | null;
  pricePerDay?: number | null;
  isFree?: boolean | null;
  condition?: ItemCondition | null;
  minLoanDays?: number | null;
  maxLoanDays?: number | null;
  requiresVerification?: boolean | null;
  pickupAddress?: string | null;
  pickupLatitude?: number | null;
  pickupLongitude?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  isActive?: boolean | null;
  availability?: ItemAvailability | null;
}

export interface AdminDecideItemDto {
  isApproved: boolean;
  adminNote?: string | null;
}

export interface AdminUpdateItemStatusDto {
  status: ItemStatus;
  adminNote?: string | null;
}

export interface ToggleActiveStatusDto {
  isActive: boolean;
}

export interface ItemDto {
  id: number;
  title: string;
  slug: string;
  description: string;
  currentValue: number;
  pricePerDay: number;
  isFree: boolean;
  condition: ItemCondition;
  isCurrentlyOnLoan: boolean;
  isMine: boolean;
  categoryId: number;
  categoryName: string;
  categorySlug: string;
  categoryIcon: string | null;
  ownerId: string;
  ownerName: string;
  ownerUserName: string;
  ownerAvatarUrl: string | null;
  ownerScore: number;
  isOwnerVerified: boolean;
  minLoanDays: number | null;
  maxLoanDays: number | null;
  requiresVerification: boolean;
  pickupAddress: string;
  pickupLatitude: number;
  pickupLongitude: number;
  availableFrom: string;
  availableUntil: string;
  status: ItemStatus;
  availability: ItemAvailability;
  isActive: boolean;
  adminNote: string | null;
  reviewedByAdminId: string | null;
  reviewedByAdminName: string | null;
  reviewedByAdminUserName: string | null;
  reviewedByAdminAvatarUrl: string | null;
  reviewedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  averageRating: number | null;
  reviewCount: number;
  totalLoans: number;
  isFavoritedByCurrentUser: boolean;
  photos: ItemPhotoDto[];
}

export interface ItemListDto {
  id: number;
  title: string;
  description: string | null;
  slug: string;
  mainPhotoUrl: string | null;
  categoryId: number;
  categoryName: string;
  categorySlug: string;
  categoryIcon: string | null;
  pickupAddress: string;
  ownerId: string;
  ownerName: string;
  ownerUsername: string;
  ownerAvatarUrl: string;
  ownerScore: number;
  isOwnerVerified: boolean;
  status: ItemStatus;
  isFree: boolean;
  pricePerDay: number;
  condition: ItemCondition;
  availability: ItemAvailability;
  isActive: boolean;
  requiresVerification: boolean;
  averageRating: number | null;
  totalReviews: number;
  maxLoanDays: number | null;
  minLoanDays: number | null;
  availableFrom: string;
  availableUntil: string;
  distanceFromUser: number | null;
  createdAt: string;
}

export interface ItemQrCodeDto {
  itemId: number;
  qrCode: string;
}