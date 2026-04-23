import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminResolveDisputeDto,
  CreateDisputeDto,
  DisputeDto,
  DisputeListDto,
  DisputeStatsDto,
  EditDisputeDto,
  SubmitDisputeResponseDto,
} from '../dtos/disputeDto';
import { AddDisputePhotoDto, DisputePhotoDto } from '../dtos/disputePhotoDto';
import { DisputeFilter } from '../dtos/filterDto';
import { DisputeStatus } from '../dtos/enums';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';

@Injectable({ providedIn: 'root' })
export class DisputeService {
  private readonly baseUrl = 'https://localhost:7183/api/disputes';

  constructor(private http: HttpClient) {}

  // Only appends params that have a real value — never sends null/undefined
  private buildParams(filter: DisputeFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                    params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null)    params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim())             params = params.set('search', filter.search.trim());
    if (filter.filedAs)                    params = params.set('filedAs', filter.filedAs);
    if (filter.status)                     params = params.set('status', filter.status);
    if (filter.adminVerdict)               params = params.set('adminVerdict', filter.adminVerdict);
    if (filter.filedById)                  params = params.set('filedById', filter.filedById);
    if (filter.respondedById)              params = params.set('respondedById', filter.respondedById);
    if (filter.resolvedByAdminId)          params = params.set('resolvedByAdminId', filter.resolvedByAdminId);
    if (filter.loanId != null)             params = params.set('loanId', filter.loanId.toString());
    if (filter.hasResponse != null)        params = params.set('hasResponse', filter.hasResponse.toString());
    if (filter.isResolved != null)         params = params.set('isResolved', filter.isResolved.toString());
    if (filter.isOverdueResponse != null)  params = params.set('isOverdueResponse', filter.isOverdueResponse.toString());
    if (filter.createdAfter)               params = params.set('createdAfter', filter.createdAfter);
    if (filter.createdBefore)              params = params.set('createdBefore', filter.createdBefore);
    if (filter.resolvedAfter)              params = params.set('resolvedAfter', filter.resolvedAfter);
    if (filter.resolvedBefore)             params = params.set('resolvedBefore', filter.resolvedBefore);
    if (filter.responseDeadlineBefore)     params = params.set('responseDeadlineBefore', filter.responseDeadlineBefore);
    if (filter.responseDeadlineAfter)      params = params.set('responseDeadlineAfter', filter.responseDeadlineAfter);
    if (filter.minCustomFine != null)      params = params.set('minCustomFine', filter.minCustomFine.toString());
    if (filter.maxCustomFine != null)      params = params.set('maxCustomFine', filter.maxCustomFine.toString());

    return params;
  }

  // ── Filing ────────────────────────────────────────────────────────────────

  createDispute(dto: CreateDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(this.baseUrl, dto);
  }

  editDispute(id: number, dto: EditDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.put<ApiResponse<DisputeDto>>(`${this.baseUrl}/${id}`, dto);
  }

  cancelDispute(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }

  // ── Evidence ─────────────────────────────────────────────────────────────

  addFiledByPhoto(id: number, dto: AddDisputePhotoDto): Observable<ApiResponse<DisputePhotoDto>> {
    return this.http.post<ApiResponse<DisputePhotoDto>>(`${this.baseUrl}/${id}/photos/filed`, dto);
  }

  deleteFiledByPhoto(id: number, photoId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}/photos/filed/${photoId}`);
  }

  addResponsePhoto(id: number, dto: AddDisputePhotoDto): Observable<ApiResponse<DisputePhotoDto>> {
    return this.http.post<ApiResponse<DisputePhotoDto>>(`${this.baseUrl}/${id}/photos/response`, dto);
  }

  // ── Other party ───────────────────────────────────────────────────────────

  markViewed(id: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/${id}/viewed`, {});
  }

  submitResponse(id: number, dto: SubmitDisputeResponseDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(`${this.baseUrl}/${id}/response`, dto);
  }

  // ── User queries ──────────────────────────────────────────────────────────

  getById(id: number): Observable<ApiResponse<DisputeDto>> {
    return this.http.get<ApiResponse<DisputeDto>>(`${this.baseUrl}/${id}`);
  }

  getMyAll(filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/all`,
      { params: this.buildParams(filter, request) }
    );
  }

  getMyFiled(filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/filed`,
      { params: this.buildParams(filter, request) }
    );
  }

  getMyResponding(filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/responding`,
      { params: this.buildParams(filter, request) }
    );
  }

  canFileDispute(loanId: number): Observable<ApiResponse<{ canFile: boolean }>> {
    return this.http.get<ApiResponse<{ canFile: boolean }>>(`${this.baseUrl}/can-file/${loanId}`);
  }

  // ── Admin queries ─────────────────────────────────────────────────────────

  adminGetById(id: number): Observable<ApiResponse<DisputeDto>> {
    return this.http.get<ApiResponse<DisputeDto>>(`${this.baseUrl}/admin/${id}`);
  }

  adminGetAll(filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/all`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminGetOpen(filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/open`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminGetByStatus(status: DisputeStatus, filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/status/${status}`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminGetByLoan(loanId: number): Observable<ApiResponse<DisputeDto[]>> {
    return this.http.get<ApiResponse<DisputeDto[]>>(`${this.baseUrl}/admin/loan/${loanId}`);
  }

  adminGetHistoryByItem(itemId: number, filter: DisputeFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/item/${itemId}/history`,
      { params: this.buildParams(filter, request) }
    );
  }

  adminResolve(id: number, dto: AdminResolveDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(`${this.baseUrl}/admin/${id}/resolve`, dto);
  }

  adminGetStats(): Observable<ApiResponse<DisputeStatsDto>> {
    return this.http.get<ApiResponse<DisputeStatsDto>>(`${this.baseUrl}/admin/stats`);
  }
}