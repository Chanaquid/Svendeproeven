import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import {
  MarkMultipleNotificationsReadDto,
  NotificationDto,
  NotificationSummaryDto,
} from '../dtos/notificationDto';
import { NotificationFilter } from '../dtos/filterDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly baseUrl = 'https://localhost:7183/api/notifications';

  constructor(private http: HttpClient) {}

  private buildParams(filter: NotificationFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                 params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.type != null)            params = params.set('type', filter.type.toString());
    if (filter.referenceType != null)   params = params.set('referenceType', filter.referenceType.toString());
    if (filter.isRead != null)          params = params.set('isRead', filter.isRead.toString());
    if (filter.referenceId != null)     params = params.set('referenceId', filter.referenceId.toString());
    if (filter.search?.trim())          params = params.set('search', filter.search.trim());
    if (filter.createdAfter)            params = params.set('createdAfter', filter.createdAfter);
    if (filter.createdBefore)           params = params.set('createdBefore', filter.createdBefore);

    return params;
  }

  // GET /api/notifications/summary
  getSummary(): Observable<ApiResponse<NotificationSummaryDto>> {
    return this.http.get<ApiResponse<NotificationSummaryDto>>(
      `${this.baseUrl}/summary`
    );
  }

  // GET /api/notifications
  getAll(
    filter: NotificationFilter | null = null,
    request: PagedRequest = { page: 1, pageSize: 15 }
  ): Observable<ApiResponse<PagedResult<NotificationDto>>> {
    return this.http.get<ApiResponse<PagedResult<NotificationDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }

  // PATCH /api/notifications/{id}/read
  markAsRead(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/${id}/read`,
      {}
    );
  }

  // PATCH /api/notifications/read-multiple
  markMultipleAsRead(dto: MarkMultipleNotificationsReadDto): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/read-multiple`,
      dto
    );
  }

  // PATCH /api/notifications/read-all
  markAllAsRead(): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/read-all`,
      {}
    );
  }

  // DELETE /api/notifications/{id}
  delete(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }

  // DELETE /api/notifications/all
  deleteAll(): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/all`);
  }
}