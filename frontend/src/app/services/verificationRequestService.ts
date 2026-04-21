import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';

import {
  VerificationRequestDto,
  CreateVerificationRequestDto,
  AdminDecideVerificationRequestDto,
} from '../dtos/verificationRequestDto';

import { VerificationRequestFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class VerificationRequestService {
  private readonly baseUrl = 'https://localhost:7183/api/verification';

  constructor(private http: HttpClient) {}

  // POST /api/verification
  submitRequest(
    dto: CreateVerificationRequestDto,
  ): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.post<ApiResponse<VerificationRequestDto>>(this.baseUrl, dto);
  }

  // GET /api/verification/my
  getMyRequests(
    filter: VerificationRequestFilter,
    request: PagedRequest,
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(`${this.baseUrl}/my`, {
      params: { ...(filter || {}), ...request } as any,
    });
  }

  // GET /api/verification/{id}
  getById(id: number): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.get<ApiResponse<VerificationRequestDto>>(`${this.baseUrl}/${id}`);
  }

  //Admin

  // GET /api/verification
  getAll(
    filter: VerificationRequestFilter,
    request: PagedRequest,
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(this.baseUrl, {
      params: { ...(filter || {}), ...request } as any,
    });
  }

  // GET /api/verification/user/{userId}
  getByUserId(
    userId: string,
    filter: VerificationRequestFilter,
    request: PagedRequest,
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: { ...(filter || {}), ...request } as any },
    );
  }

  // POST /api/verification/{id}/decide
  decide(
    id: number,
    dto: AdminDecideVerificationRequestDto,
  ): Observable<ApiResponse<VerificationRequestDto>> {
    return this.http.post<ApiResponse<VerificationRequestDto>>(`${this.baseUrl}/${id}/decide`, dto);
  }
}
