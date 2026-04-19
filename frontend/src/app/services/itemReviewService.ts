import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminCreateItemReviewDto,
  CreateItemReviewDto,
  ItemReviewDto,
  UpdateItemReviewDto,
} from '../dtos/itemReviewDto';
import { ItemReviewFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ItemReviewService {
  private readonly baseUrl = 'https://localhost:7183/api/items';

  constructor(private http: HttpClient) {}

  // GET /api/items/{itemId}/reviews
  getByItem(
    itemId: number,
    filter: ItemReviewFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemReviewDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemReviewDto>>>(
      `${this.baseUrl}/${itemId}/reviews`,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/items/{itemId}/reviews
  create(itemId: number, dto: CreateItemReviewDto): Observable<ApiResponse<ItemReviewDto>> {
    return this.http.post<ApiResponse<ItemReviewDto>>(
      `${this.baseUrl}/${itemId}/reviews`,
      { ...dto, itemId }
    );
  }

  // PUT /api/items/{itemId}/reviews/{reviewId}
  edit(
    itemId: number,
    reviewId: number,
    dto: UpdateItemReviewDto
  ): Observable<ApiResponse<ItemReviewDto>> {
    return this.http.put<ApiResponse<ItemReviewDto>>(
      `${this.baseUrl}/${itemId}/reviews/${reviewId}`,
      dto
    );
  }

  // DELETE /api/items/{itemId}/reviews/{reviewId}  [Admin]
  adminDelete(itemId: number, reviewId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/${itemId}/reviews/${reviewId}`
    );
  }

  // POST /api/items/{itemId}/reviews/admin  [Admin]
  adminCreate(
    itemId: number,
    dto: AdminCreateItemReviewDto
  ): Observable<ApiResponse<ItemReviewDto>> {
    return this.http.post<ApiResponse<ItemReviewDto>>(
      `${this.baseUrl}/${itemId}/reviews/admin`,
      dto
    );
  }
}