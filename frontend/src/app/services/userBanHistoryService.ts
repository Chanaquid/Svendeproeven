import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import { UserBanHistoryDto } from '../dtos/userBanHistoryDto';
import { UserBanHistoryFilter } from '../dtos/filterDto'

@Injectable({
  providedIn: 'root',
})
export class UserBanHistoryService {
  private readonly baseUrl = 'https://localhost:7183/api/ban-history';

  constructor(private http: HttpClient) {}

  // GET /api/ban-history  [Admin]
  getAll(
    filter: UserBanHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBanHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBanHistoryDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/ban-history/{id}  [Admin]
  getById(id: number): Observable<ApiResponse<UserBanHistoryDto>> {
    return this.http.get<ApiResponse<UserBanHistoryDto>>(
      `${this.baseUrl}/${id}`
    );
  }

  // GET /api/ban-history/user/{userId}  [Admin]
  getByUserId(
    userId: string,
    filter: UserBanHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBanHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBanHistoryDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: { ...filter, ...request } as any }
    );
  }
}