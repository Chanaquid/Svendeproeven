import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminAppealDto,
  AdminDecidesFineAppealDto,
  AdminDecidesScoreAppealDto,
  AppealDto,
  CreateFineAppealDto,
  CreateScoreAppealDto,
} from '../dtos/appealDto';
import { AppealFilter } from '../dtos/filterDto';
import { AppealStatus, AppealType } from '../dtos/enums';

@Injectable({ providedIn: 'root' })
export class AppealService {
  private readonly baseUrl = 'https://localhost:7183/api/appeals';

  constructor(private http: HttpClient) {}

  private buildParams(filter: AppealFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                 params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim())          params = params.set('search', filter.search.trim());
    if (filter.status)                  params = params.set('status', filter.status);
    if (filter.appealType)              params = params.set('appealType', filter.appealType);
    if (filter.userId)                  params = params.set('userId', filter.userId);
    if (filter.resolvedByAdminId)       params = params.set('resolvedByAdminId', filter.resolvedByAdminId);
    if (filter.fineId != null)          params = params.set('fineId', filter.fineId.toString());
    if (filter.scoreHistoryId != null)  params = params.set('scoreHistoryId', filter.scoreHistoryId.toString());
    if (filter.isResolved != null)      params = params.set('isResolved', filter.isResolved.toString());
    if (filter.createdAfter)            params = params.set('createdAfter', filter.createdAfter);
    if (filter.createdBefore)           params = params.set('createdBefore', filter.createdBefore);
    if (filter.resolvedAfter)           params = params.set('resolvedAfter', filter.resolvedAfter);
    if (filter.resolvedBefore)          params = params.set('resolvedBefore', filter.resolvedBefore);

    return params;
  }

  // ── User endpoints ────────────────────────────────────────────────────────

  getMyAppeals(filter: AppealFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/my`,
      { params: this.buildParams(filter, request) }
    );
  }

  // Regular user view — returns base AppealDto
  getById(id: number): Observable<ApiResponse<AppealDto>> {
    return this.http.get<ApiResponse<AppealDto>>(`${this.baseUrl}/${id}`);
  }

  createScoreAppeal(dto: CreateScoreAppealDto): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(`${this.baseUrl}/score`, dto);
  }

  createFineAppeal(dto: CreateFineAppealDto): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(`${this.baseUrl}/fine`, dto);
  }

  cancelAppeal(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(`${this.baseUrl}/${id}/cancel`, {});
  }

  deleteAppeal(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }

  // ── Admin endpoints ───────────────────────────────────────────────────────

  adminGetAll(filter: AppealFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }

  adminGetPending(filter: AppealFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/pending`,
      { params: this.buildParams(filter, request) }
    );
  }

  // Admin detail view — returns AdminAppealDto with user stats (score, fines, borrows, etc.)
  adminGetById(id: number): Observable<ApiResponse<AdminAppealDto>> {
    return this.http.get<ApiResponse<AdminAppealDto>>(`${this.baseUrl}/admin/${id}`);
  }

  adminGetByUserId(userId: string, filter: AppealFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminGetByStatus(status: AppealStatus, filter: AppealFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/status/${status}`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminDecideScore(id: number, dto: AdminDecidesScoreAppealDto): Observable<ApiResponse<AdminAppealDto>> {
    return this.http.post<ApiResponse<AdminAppealDto>>(`${this.baseUrl}/${id}/decide/score`, dto);
  }

  adminDecideFine(id: number, dto: AdminDecidesFineAppealDto): Observable<ApiResponse<AdminAppealDto>> {
    return this.http.post<ApiResponse<AdminAppealDto>>(`${this.baseUrl}/${id}/decide/fine`, dto);
  }
}