import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import { UserBlockDto, UserBlockListDto } from '../dtos/userBlockDto';
import { UserBlockFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class UserBlockService {
  private readonly baseUrl = 'https://localhost:7183/api/blocks';

  constructor(private http: HttpClient) {}

  // POST /api/blocks/{userId}
  blockUser(userId: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/${userId}`,
      {}
    );
  }

  // DELETE /api/blocks/{userId}
  unblockUser(userId: string): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/${userId}`
    );
  }

  // GET /api/blocks/my
  getMyBlocks(
    filter: UserBlockFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBlockListDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBlockListDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/blocks/status/{userId}
  checkBlockStatus(userId: string): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(
      `${this.baseUrl}/status/${userId}`
    );
  }

  //Admin

  // GET /api/blocks
  getAll(
    filter: UserBlockFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBlockDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBlockDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // DELETE /api/blocks/admin/{blockerId}/{blockedId}
  adminUnblock(
    blockerId: string,
    blockedId: string
  ): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/admin/${blockerId}/${blockedId}`
    );
  }
}