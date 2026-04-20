import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import {
  MarkMultipleNotificationsReadDto,
  NotificationDto,
  NotificationSummaryDto,
} from '../dtos/notificationDTO';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly baseUrl = 'https://localhost:7183/api/notifications';

  constructor(private http: HttpClient) {}

  // GET /api/notifications/summary
  getSummary(): Observable<ApiResponse<NotificationSummaryDto>> {
    return this.http.get<ApiResponse<NotificationSummaryDto>>(
      `${this.baseUrl}/summary`
    );
  }

  // GET /api/notifications
  getAll(): Observable<ApiResponse<NotificationDto[]>> {
    return this.http.get<ApiResponse<NotificationDto[]>>(this.baseUrl);
  }

  // PATCH /api/notifications/{id}/read
  markAsRead(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/${id}/read`,
      {}
    );
  }

  // PATCH /api/notifications/read-multiple
  markMultipleAsRead(
    dto: MarkMultipleNotificationsReadDto
  ): Observable<ApiResponse<string>> {
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