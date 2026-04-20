import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminDecidesFineAppealDto,
  AdminDecidesScoreAppealDto,
  AppealDto,
  CreateFineAppealDto,
  CreateScoreAppealDto,
} from '../dtos/appealDTO';
import { AppealFilter } from '../dtos/filterDto';
import { AppealStatus } from '../dtos/enums';

@Injectable({
  providedIn: 'root',
})
export class AppealService {
  private readonly baseUrl = 'https://localhost:7183/api/appeals';

  constructor(private http: HttpClient) {}

  //User endpoints

  // GET /api/appeals/my
  getMyAppeals(
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/appeals/{id}
  getById(id: number): Observable<ApiResponse<AppealDto>> {
    return this.http.get<ApiResponse<AppealDto>>(`${this.baseUrl}/${id}`);
  }

  // POST /api/appeals/score
  createScoreAppeal(dto: CreateScoreAppealDto): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(`${this.baseUrl}/score`, dto);
  }

  // POST /api/appeals/fine
  createFineAppeal(dto: CreateFineAppealDto): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(`${this.baseUrl}/fine`, dto);
  }

  // PATCH /api/appeals/{id}/cancel
  cancelAppeal(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(`${this.baseUrl}/${id}/cancel`, {});
  }

  // DELETE /api/appeals/{id}
  deleteAppeal(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }

  //Admin endpoints

  // GET /api/appeals
  adminGetAll(
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/appeals/pending
  adminGetPending(
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/pending`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/appeals/user/{userId}
  adminGetByUserId(
    userId: string,
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/appeals/status/{status}
  adminGetByStatus(
    status: AppealStatus,
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.baseUrl}/status/${status}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/appeals/{id}/decide/score
  adminDecideScore(
    id: number,
    dto: AdminDecidesScoreAppealDto
  ): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(
      `${this.baseUrl}/${id}/decide/score`,
      dto
    );
  }

  // POST /api/appeals/{id}/decide/fine
  adminDecideFine(
    id: number,
    dto: AdminDecidesFineAppealDto
  ): Observable<ApiResponse<AppealDto>> {
    return this.http.post<ApiResponse<AppealDto>>(
      `${this.baseUrl}/${id}/decide/fine`,
      dto
    );
  }
}