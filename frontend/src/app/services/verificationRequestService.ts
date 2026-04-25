import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  VerificationRequestDto,
  CreateVerificationRequestDto,
  AdminDecideVerificationRequestDto,
} from '../dtos/verificationRequestDto';
import { VerificationRequestFilter } from '../dtos/filterDto';

@Injectable({ providedIn: 'root' })
export class VerificationRequestService {
  private readonly baseUrl = 'https://localhost:7183/api/verification';

  constructor(private http: HttpClient) {}

  // Only appends params that have a real value — never sends null/undefined/"null"
  private buildParams(filter: VerificationRequestFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                 params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim())          params = params.set('search', filter.search.trim());
    if (filter.status)                  params = params.set('status', filter.status);
    if (filter.documentType)            params = params.set('documentType', filter.documentType);
    if (filter.userId)                  params = params.set('userId', filter.userId);
    if (filter.reviewedByAdminId)       params = params.set('reviewedByAdminId', filter.reviewedByAdminId);
    if (filter.isReviewed != null)      params = params.set('isReviewed', filter.isReviewed.toString());
    if (filter.isApproved != null)      params = params.set('isApproved', filter.isApproved.toString());
    if (filter.isRejected != null)      params = params.set('isRejected', filter.isRejected.toString());
    if (filter.submittedAfter)          params = params.set('submittedAfter', filter.submittedAfter);
    if (filter.submittedBefore)         params = params.set('submittedBefore', filter.submittedBefore);
    if (filter.reviewedAfter)           params = params.set('reviewedAfter', filter.reviewedAfter);
    if (filter.reviewedBefore)          params = params.set('reviewedBefore', filter.reviewedBefore);

    return params;
  }

  // ── User ──────────────────────────────────────────────────────────────────

  submitRequest(dto: CreateVerificationRequestDto): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.post<ApiResponse<VerificationRequestDto>>(this.baseUrl, dto);
  }

  getMyRequests(
    filter: VerificationRequestFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(
      `${this.baseUrl}/my`,
      { params: this.buildParams(filter, request) }
    );
  }

  getById(id: number): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.get<ApiResponse<VerificationRequestDto>>(`${this.baseUrl}/${id}`);
  }

  // ── Admin ─────────────────────────────────────────────────────────────────

  getAll(
    filter: VerificationRequestFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }

  getByUserId(
    userId: string,
    filter: VerificationRequestFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: this.buildParams(filter, request) }
    );
  }

  decide(
    id: number,
    dto: AdminDecideVerificationRequestDto
  ): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.post<ApiResponse<VerificationRequestDto>>(`${this.baseUrl}/${id}/decide`, dto);
  }
}