import { HttpClient, HttpParams } from '@angular/common/http';
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

  // Only appends params that have a real value — never sends null/undefined
  private buildParams(filter: UserBlockFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)               params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim())        params = params.set('search', filter.search.trim());
    if (filter.blockerId)             params = params.set('blockerId', filter.blockerId);
    if (filter.blockedId)             params = params.set('blockedId', filter.blockedId);
    if (filter.createdAfter)          params = params.set('createdAfter', filter.createdAfter);
    if (filter.createdBefore)         params = params.set('createdBefore', filter.createdBefore);

    return params;
  }

  // POST /api/blocks/{userId}
  blockUser(userId: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/${userId}`, {});
  }

  // DELETE /api/blocks/{userId}
  unblockUser(userId: string): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${userId}`);
  }

  // GET /api/blocks/my
  getMyBlocks(
    filter: UserBlockFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBlockListDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBlockListDto>>>(
      `${this.baseUrl}/my`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/blocks/status/{userId}
  checkBlockStatus(userId: string): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(`${this.baseUrl}/status/${userId}`);
  }

  // GET /api/blocks (Admin)
  getAll(
    filter: UserBlockFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserBlockDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserBlockDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }

  // DELETE /api/blocks/admin/{blockerId}/{blockedId} (Admin)
  adminUnblock(
    blockerId: string,
    blockedId: string
  ): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/admin/${blockerId}/${blockedId}`
    );
  }
}