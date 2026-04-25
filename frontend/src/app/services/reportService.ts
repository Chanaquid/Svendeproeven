import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminResolveReportDto,
  CreateReportDto,
  ReportDto,
} from '../dtos/reportDto';
import { ReportFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly baseUrl = 'https://localhost:7183/api/reports';

  constructor(private http: HttpClient) {}

  // Only appends params that have a real value — never sends null/undefined
  private buildParams(filter: ReportFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)               params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim())        params = params.set('search', filter.search.trim());
    if (filter.reportedById)          params = params.set('reportedById', filter.reportedById);
    if (filter.handledByAdminId)      params = params.set('handledByAdminId', filter.handledByAdminId);
    if (filter.type)                  params = params.set('type', filter.type);
    if (filter.reasons)               params = params.set('reasons', filter.reasons);
    if (filter.status)                params = params.set('status', filter.status);
    if (filter.targetId)              params = params.set('targetId', filter.targetId);
    if (filter.isResolved != null)    params = params.set('isResolved', filter.isResolved.toString());
    if (filter.createdAfter)          params = params.set('createdAfter', filter.createdAfter);
    if (filter.createdBefore)         params = params.set('createdBefore', filter.createdBefore);
    if (filter.resolvedAfter)         params = params.set('resolvedAfter', filter.resolvedAfter);
    if (filter.resolvedBefore)        params = params.set('resolvedBefore', filter.resolvedBefore);

    return params;
  }

  // ── User endpoints ────────────────────────────────────────────────────────

  // POST /api/reports
  create(dto: CreateReportDto): Observable<ApiResponse<ReportDto>> {
    return this.http.post<ApiResponse<ReportDto>>(this.baseUrl, dto);
  }

  // GET /api/reports/my
  getMy(
    filter: ReportFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ReportDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReportDto>>>(
      `${this.baseUrl}/my`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/reports/{id}
  getById(id: number): Observable<ApiResponse<ReportDto>> {
    return this.http.get<ApiResponse<ReportDto>>(`${this.baseUrl}/${id}`);
  }

  // ── Admin endpoints ───────────────────────────────────────────────────────

  // GET /api/reports
  adminGetAll(
    filter: ReportFilter | null,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ReportDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReportDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }

  // POST /api/reports/{id}/resolve
  adminResolve(
    id: number,
    dto: AdminResolveReportDto
  ): Observable<ApiResponse<ReportDto>> {
    return this.http.post<ApiResponse<ReportDto>>(
      `${this.baseUrl}/${id}/resolve`,
      dto
    );
  }
}