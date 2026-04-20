import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminDecideItemDto,
  AdminUpdateItemStatusDto,
  CreateItemDto,
  ItemDto,
  ItemListDto,
  ItemQrCodeDto,
  ToggleActiveStatusDto,
  UpdateItemDto,
} from '../dtos/itemDTO';
import { AddItemPhotoDto } from '../dtos/itemPhotoDto';
import { ItemFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ItemService {
  private readonly baseUrl = 'https://localhost:7183/api/items';

  constructor(private http: HttpClient) {}

  //Public user endpoints

  // GET /api/items/landing?count=4
  getLatest(count = 4): Observable<ApiResponse<ItemListDto[]>> {
    return this.http.get<ApiResponse<ItemListDto[]>>(
      `${this.baseUrl}/landing`,
      { params: { count } as any }
    );
  }

  // GET /api/items
  getAllApproved(
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/available
  getAvailable(
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/available`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/nearby?lat=&lon=&radiusKm=
  getNearby(
    lat: number,
    lon: number,
    radiusKm = 10,
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/nearby`,
      { params: { lat, lon, radiusKm, ...filter, ...request } as any }
    );
  }

  // GET /api/items/category/{categoryId}
  getByCategory(
    categoryId: number,
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/category/${categoryId}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/by-owner/{ownerId}
  getPublicByOwner(
    ownerId: string,
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/by-owner/${ownerId}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/{id}
  getById(id: number): Observable<ApiResponse<ItemDto>> {
    return this.http.get<ApiResponse<ItemDto>>(`${this.baseUrl}/${id}`);
  }

  // GET /api/items/slug/{slug}
  getBySlug(slug: string): Observable<ApiResponse<ItemDto>> {
    return this.http.get<ApiResponse<ItemDto>>(`${this.baseUrl}/slug/${slug}`);
  }

  // GET /api/items/count/available
  getAvailableCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(
      `${this.baseUrl}/count/available`
    );
  }

  //Owner endpoints

  // GET /api/items/my
  getMyItems(
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/items
  create(dto: CreateItemDto): Observable<ApiResponse<ItemDto>> {
    return this.http.post<ApiResponse<ItemDto>>(this.baseUrl, dto);
  }

  // PUT /api/items/{id}
  update(id: number, dto: UpdateItemDto): Observable<ApiResponse<ItemDto>> {
    return this.http.put<ApiResponse<ItemDto>>(`${this.baseUrl}/${id}`, dto);
  }

  // DELETE /api/items/{id}
  delete(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }

  // PATCH /api/items/{id}/active
  toggleActive(id: number, dto: ToggleActiveStatusDto): Observable<ApiResponse<ItemDto>> {
    return this.http.patch<ApiResponse<ItemDto>>(
      `${this.baseUrl}/${id}/active`,
      dto
    );
  }

  // GET /api/items/{id}/qrcode
  getQrCode(id: number): Observable<ApiResponse<ItemQrCodeDto>> {
    return this.http.get<ApiResponse<ItemQrCodeDto>>(
      `${this.baseUrl}/${id}/qrcode`
    );
  }

  //Item photo endpoints

  // POST /api/items/{id}/photos
  addPhoto(id: number, dto: AddItemPhotoDto): Observable<ApiResponse<ItemDto>> {
    return this.http.post<ApiResponse<ItemDto>>(
      `${this.baseUrl}/${id}/photos`,
      dto
    );
  }

  // DELETE /api/items/{id}/photos/{photoId}
  deletePhoto(id: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.http.delete<ApiResponse<ItemDto>>(
      `${this.baseUrl}/${id}/photos/${photoId}`
    );
  }

  // PATCH /api/items/{id}/photos/{photoId}/primary
  setPrimaryPhoto(id: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.http.patch<ApiResponse<ItemDto>>(
      `${this.baseUrl}/${id}/photos/${photoId}/primary`,
      {}
    );
  }

  //Admin endpoints

  // GET /api/items/admin/all
  adminGetAll(
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/admin/all`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/admin/{id}
  adminGetById(id: number): Observable<ApiResponse<ItemDto>> {
    return this.http.get<ApiResponse<ItemDto>>(`${this.baseUrl}/admin/${id}`);
  }

  // GET /api/items/admin/pending
  adminGetPending(
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.baseUrl}/admin/pending`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/items/admin/pending/count
  adminGetPendingCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(
      `${this.baseUrl}/admin/pending/count`
    );
  }

  // POST /api/items/admin/{id}/decide
  adminDecide(id: number, dto: AdminDecideItemDto): Observable<ApiResponse<ItemDto>> {
    return this.http.post<ApiResponse<ItemDto>>(
      `${this.baseUrl}/admin/${id}/decide`,
      dto
    );
  }

  // PATCH /api/items/admin/{id}/status
  adminUpdateStatus(
    id: number,
    dto: AdminUpdateItemStatusDto
  ): Observable<ApiResponse<ItemDto>> {
    return this.http.patch<ApiResponse<ItemDto>>(
      `${this.baseUrl}/admin/${id}/status`,
      dto
    );
  }
}