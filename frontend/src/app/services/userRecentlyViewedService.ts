import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse } from '../dtos/apiResponseDto';
import { UserRecentlyViewedItemDto } from '../dtos/userRecentlyViewedItemDto';

@Injectable({
  providedIn: 'root',
})
export class UserRecentlyViewedService {
  private readonly baseUrl = 'https://localhost:7183/api/recently-viewed';

  constructor(private http: HttpClient) {}

  // GET /api/recently-viewed
  getRecentlyViewed(): Observable<ApiResponse<UserRecentlyViewedItemDto[]>> {
    return this.http.get<ApiResponse<UserRecentlyViewedItemDto[]>>(
      this.baseUrl
    );
  }

  // POST /api/recently-viewed/{itemId}
  trackView(itemId: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/${itemId}`,
      {}
    );
  }
}