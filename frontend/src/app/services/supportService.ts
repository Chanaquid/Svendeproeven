import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  CreateSupportThreadDto,
  SupportThreadDto,
  SupportThreadListDto,
} from '../dtos/supportThreadDto';
import {
  MarkSupportMessagesReadDto,
  SendSupportMessageDto,
  SupportMessageDto,
} from '../dtos/supportMessageDto';
import { SupportThreadFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class SupportService {
  private readonly baseUrl = 'https://localhost:7183/api/support';

  constructor(private http: HttpClient) {}

  // User endpoints

  // POST /api/support
  createThread(dto: CreateSupportThreadDto): Observable<ApiResponse<SupportThreadDto>> {
    return this.http.post<ApiResponse<SupportThreadDto>>(this.baseUrl, dto);
  }

  // GET /api/support/my
  getMyThreads(
    filter: SupportThreadFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<SupportThreadListDto>>> {
    return this.http.get<ApiResponse<PagedResult<SupportThreadListDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/support/{id}
  getById(id: number): Observable<ApiResponse<SupportThreadDto>> {
    return this.http.get<ApiResponse<SupportThreadDto>>(`${this.baseUrl}/${id}`);
  }

  // POST /api/support/{id}/messages
  sendMessage(
    id: number,
    dto: SendSupportMessageDto
  ): Observable<ApiResponse<SupportMessageDto>> {
    return this.http.post<ApiResponse<SupportMessageDto>>(
      `${this.baseUrl}/${id}/messages`,
      dto
    );
  }

  // PATCH /api/support/{id}/close
  closeThread(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/${id}/close`,
      {}
    );
  }

  // PATCH /api/support/{id}/read
  markAsRead(
    id: number,
    dto: MarkSupportMessagesReadDto
  ): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/${id}/read`,
      dto
    );
  }

  // Admin endpoints

  // GET /api/support
  adminGetAll(
    filter: SupportThreadFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<SupportThreadListDto>>> {
    return this.http.get<ApiResponse<PagedResult<SupportThreadListDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/support/admin/user/{userId}
  adminCreateThread(
    userId: string,
    dto: CreateSupportThreadDto
  ): Observable<ApiResponse<SupportThreadDto>> {
    return this.http.post<ApiResponse<SupportThreadDto>>(
      `${this.baseUrl}/admin/user/${userId}`,
      dto
    );
  }

  // POST /api/support/{id}/claim
  adminClaimThread(id: number): Observable<ApiResponse<SupportThreadDto>> {
    return this.http.post<ApiResponse<SupportThreadDto>>(
      `${this.baseUrl}/${id}/claim`,
      {}
    );
  }
}