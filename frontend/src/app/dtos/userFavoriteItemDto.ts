import { ItemAvailability, ItemCondition } from './enums';
import { ItemDto } from './itemDto';

export interface FavoriteResponseDto {
  item: ItemDto;
  notifyWhenAvailable: boolean;
  savedAt: string;
}

export interface UserFavoriteItemListDto {
  id: number;
  title: string;
  description: string | null;
  slug: string;
  mainPhotoUrl: string | null;
  pickupAddress: string;
  categoryId: number;
  categoryName: string;
  categorySlug: string;
  categoryIcon: string | null;
  ownerId: string;
  ownerName: string;
  ownerUsername: string;
  ownerAvatarUrl: string;
  ownerScore: number;
  isOwnerVerified: boolean;
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
  notifyWhenAvailable: boolean;
  savedAt: string;
}

export interface FavoriteToggleResultDto {
  itemId: number;
  isFavorited: boolean;
}

export interface FavoriteStatusDto {
  itemId: number;
  isFavorited: boolean;
}

export interface NotifyPreferenceResultDto {
  itemId: number;
  notifyWhenAvailable: boolean;
}

export interface UpdateNotifyPreferenceDto {
  notify: boolean;
}