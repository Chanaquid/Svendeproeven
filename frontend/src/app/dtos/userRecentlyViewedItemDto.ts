import { ItemDto } from './itemDTO';

export interface UserRecentlyViewedItemResponseDto {
  item: ItemDto;
  viewedAt: string;
}

export interface UserRecentlyViewedItemDto {
  itemId: number;
  itemTitle: string;
  itemSlug: string;
  itemMainPhotoUrl: string | null;
  pricePerDay: number;
  isFree: boolean;
  isAvailable: boolean;
  ownerName: string;
  viewedAt: string;
}