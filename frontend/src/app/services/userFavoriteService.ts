import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';

import {
  FavoriteToggleResultDto,
  FavoriteStatusDto,
  NotifyPreferenceResultDto,
  UpdateNotifyPreferenceDto
} from '../dtos/userFavoriteItemDto';
import { ItemListDto } from '../dtos/itemDto';

@Injectable({
  providedIn: 'root',
})
export class UserFavoriteService {
  private readonly baseUrl = 'https://localhost:7183/api/favorites';

  constructor(private http: HttpClient) {}

  // GET /api/favorites
  getMyFavorites(
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      this.baseUrl,
      { params: { ...request } as any }
    );
  }

  // POST /api/favorites/{itemId}?notify=true/false
  toggle(
    itemId: number,
    notify: boolean = false
  ): Observable<ApiResponse<FavoriteToggleResultDto>> {
    return this.http.post<ApiResponse<FavoriteToggleResultDto>>(
      `${this.baseUrl}/${itemId}`,
      {},
      { params: { notify } }
    );
  }

  // GET /api/favorites/{itemId}/status
  getStatus(
    itemId: number
  ): Observable<ApiResponse<FavoriteStatusDto>> {
    return this.http.get<ApiResponse<FavoriteStatusDto>>(
      `${this.baseUrl}/${itemId}/status`
    );
  }

  // PATCH /api/favorites/{itemId}/notify
  updateNotifyPreference(
    itemId: number,
    dto: UpdateNotifyPreferenceDto
  ): Observable<ApiResponse<NotifyPreferenceResultDto>> {
    return this.http.patch<ApiResponse<NotifyPreferenceResultDto>>(
      `${this.baseUrl}/${itemId}/notify`,
      dto
    );
  }
}