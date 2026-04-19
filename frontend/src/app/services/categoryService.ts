import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { CategoryDto, CreateCategoryDto, UpdateCategoryDto } from '../dtos/categoryDto';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private readonly baseUrl = 'https://localhost:7183/api/categories';

  constructor(private http: HttpClient) {}

  // GET /api/categories
  getAll(): Observable<ApiResponse<CategoryDto[]>> {
    return this.http.get<ApiResponse<CategoryDto[]>>(this.baseUrl);
  }

  // GET /api/categories/{id}
  getById(id: number): Observable<ApiResponse<CategoryDto>> {
    return this.http.get<ApiResponse<CategoryDto>>(`${this.baseUrl}/${id}`);
  }

  // GET /api/categories/slug/{slug}
  getBySlug(slug: string): Observable<ApiResponse<CategoryDto>> {
    return this.http.get<ApiResponse<CategoryDto>>(`${this.baseUrl}/slug/${slug}`);
  }

  // POST /api/categories  [Admin]
  create(dto: CreateCategoryDto): Observable<ApiResponse<CategoryDto>> {
    return this.http.post<ApiResponse<CategoryDto>>(this.baseUrl, dto);
  }

  // PUT /api/categories/{id}  [Admin]
  update(id: number, dto: UpdateCategoryDto): Observable<ApiResponse<CategoryDto>> {
    return this.http.put<ApiResponse<CategoryDto>>(`${this.baseUrl}/${id}`, dto);
  }

  // PATCH /api/categories/{id}/toggle  [Admin]
  toggle(id: number): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(`${this.baseUrl}/${id}/toggle`, {});
  }

  // DELETE /api/categories/{id}  [Admin]
  delete(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`);
  }
}