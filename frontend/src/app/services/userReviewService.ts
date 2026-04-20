import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';

import {
  UserReviewDto,
  UserReviewListDto,
  CreateUserReviewDto,
  UpdateUserReviewDto,
  AdminCreateUserReviewDto,
  UserRatingSummaryDto
} from '../dtos/userReviewDto';

import { UserReviewFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class UserReviewService {
  private readonly baseUrl = 'https://localhost:7183/api/user-reviews';

  constructor(private http: HttpClient) {}

  // POST /api/user-reviews
  createReview(
    dto: CreateUserReviewDto
  ): Observable<ApiResponse<UserReviewDto>> {
    return this.http.post<ApiResponse<UserReviewDto>>(
      this.baseUrl,
      dto
    );
  }

  // PATCH /api/user-reviews/{id}
  updateReview(
    id: number,
    dto: UpdateUserReviewDto
  ): Observable<ApiResponse<UserReviewDto>> {
    return this.http.patch<ApiResponse<UserReviewDto>>(
      `${this.baseUrl}/${id}`,
      dto
    );
  }

  // GET /api/user-reviews/{id}
  getById(id: number): Observable<ApiResponse<UserReviewDto>> {
    return this.http.get<ApiResponse<UserReviewDto>>(
      `${this.baseUrl}/${id}`
    );
  }

  // GET /api/user-reviews/user/{userId}
  getReviewsForUser(
    userId: string,
    filter: UserReviewFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserReviewListDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserReviewListDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: { ...(filter || {}), ...request } as any }
    );
  }

  // GET /api/user-reviews/user/{userId}/summary
  getRatingSummary(
    userId: string
  ): Observable<ApiResponse<UserRatingSummaryDto>> {
    return this.http.get<ApiResponse<UserRatingSummaryDto>>(
      `${this.baseUrl}/user/${userId}/summary`
    );
  }

  // GET /api/user-reviews/my/given
  getMyGivenReviews(
    filter: UserReviewFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserReviewListDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserReviewListDto>>>(
      `${this.baseUrl}/my/given`,
      { params: { ...(filter || {}), ...request } as any }
    );
  }

  //Admin endpoints

  // GET /api/user-reviews
  getAll(
    filter: UserReviewFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserReviewDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserReviewDto>>>(
      this.baseUrl,
      { params: { ...(filter || {}), ...request } as any }
    );
  }

  // POST /api/user-reviews/admin
  adminCreateReview(
    dto: AdminCreateUserReviewDto
  ): Observable<ApiResponse<UserReviewDto>> {
    return this.http.post<ApiResponse<UserReviewDto>>(
      `${this.baseUrl}/admin`,
      dto
    );
  }

  // DELETE /api/user-reviews/{id}
  adminDeleteReview(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/${id}`
    );
  }
}