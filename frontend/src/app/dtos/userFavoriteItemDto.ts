import { ItemDto } from './itemDTO';

export interface FavoriteResponseDto {
  item: ItemDto;
  notifyWhenAvailable: boolean;
  savedAt: string;
}

export interface ToggleNotifyDto {
  notifyWhenAvailable: boolean;
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