import { HttpClient } from '@angular/common/http';
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

@Injectable({
  providedIn: 'root',
})
export class DisputeService {
  private readonly baseUrl = 'https://localhost:7183/api/disputes';

  constructor(private http: HttpClient) {}


  // POST /api/disputes
  createDispute(dto: CreateDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(this.baseUrl, dto);
  }

  // PUT /api/disputes/{id}
  editDispute(id: number, dto: EditDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.put<ApiResponse<DisputeDto>>(`${this.baseUrl}/${id}`, dto);
  }

  // DELETE /api/disputes/{id}
  cancelDispute(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }


  // POST /api/disputes/{id}/photos/filed
  addFiledByPhoto(id: number, dto: AddDisputePhotoDto): Observable<ApiResponse<DisputePhotoDto>> {
    return this.http.post<ApiResponse<DisputePhotoDto>>(
      `${this.baseUrl}/${id}/photos/filed`,
      dto
    );
  }

  // DELETE /api/disputes/{id}/photos/filed/{photoId}
  deleteFiledByPhoto(id: number, photoId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/${id}/photos/filed/${photoId}`
    );
  }


  // POST /api/disputes/{id}/viewed
  markViewed(id: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/${id}/viewed`, {});
  }

  // POST /api/disputes/{id}/response
  submitResponse(id: number, dto: SubmitDisputeResponseDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(
      `${this.baseUrl}/${id}/response`,
      dto
    );
  }

  // POST /api/disputes/{id}/photos/response
  addResponsePhoto(id: number, dto: AddDisputePhotoDto): Observable<ApiResponse<DisputePhotoDto>> {
    return this.http.post<ApiResponse<DisputePhotoDto>>(
      `${this.baseUrl}/${id}/photos/response`,
      dto
    );
  }

  //User queries

  // GET /api/disputes/{id}
  getById(id: number): Observable<ApiResponse<DisputeDto>> {
    return this.http.get<ApiResponse<DisputeDto>>(`${this.baseUrl}/${id}`);
  }

  // GET /api/disputes/my/filed
  getMyFiled(
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/filed`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/my/responding
  getMyResponding(
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/responding`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/my/all
  getMyAll(
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/my/all`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/can-file/{loanId}
  canFileDispute(loanId: number): Observable<ApiResponse<{ canFile: boolean }>> {
    return this.http.get<ApiResponse<{ canFile: boolean }>>(
      `${this.baseUrl}/can-file/${loanId}`
    );
  }

  //Admin queries

  // GET /api/disputes/admin/{id}
  adminGetById(id: number): Observable<ApiResponse<DisputeDto>> {
    return this.http.get<ApiResponse<DisputeDto>>(`${this.baseUrl}/admin/${id}`);
  }

  // GET /api/disputes/admin/all
  adminGetAll(
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/all`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/admin/open
  adminGetOpen(
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/open`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/admin/status/{status}
  adminGetByStatus(
    status: DisputeStatus,
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/status/${status}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/disputes/admin/loan/{loanId}
  adminGetByLoan(loanId: number): Observable<ApiResponse<DisputeDto[]>> {
    return this.http.get<ApiResponse<DisputeDto[]>>(
      `${this.baseUrl}/admin/loan/${loanId}`
    );
  }

  // GET /api/disputes/admin/item/{itemId}/history
  adminGetHistoryByItem(
    itemId: number,
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.baseUrl}/admin/item/${itemId}/history`,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/disputes/admin/{id}/resolve
  adminResolve(id: number, dto: AdminResolveDisputeDto): Observable<ApiResponse<DisputeDto>> {
    return this.http.post<ApiResponse<DisputeDto>>(
      `${this.baseUrl}/admin/${id}/resolve`,
      dto
    );
  }

  // GET /api/disputes/admin/stats
  adminGetStats(): Observable<ApiResponse<DisputeStatsDto>> {
    return this.http.get<ApiResponse<DisputeStatsDto>>(`${this.baseUrl}/admin/stats`);
  }
}