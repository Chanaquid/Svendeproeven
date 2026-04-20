import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminResolveReportDto,
  CreateReportDto,
  ReportDto,
} from '../dtos/reportDto';
import { ReportFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly baseUrl = 'https://localhost:7183/api/reports';

  constructor(private http: HttpClient) {}

  //User endpoints

  // POST /api/reports
  create(dto: CreateReportDto): Observable<ApiResponse<ReportDto>> {
    return this.http.post<ApiResponse<ReportDto>>(this.baseUrl, dto);
  }

  // GET /api/reports/my
  getMy(
    filter: ReportFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ReportDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReportDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/reports/{id}
  getById(id: number): Observable<ApiResponse<ReportDto>> {
    return this.http.get<ApiResponse<ReportDto>>(`${this.baseUrl}/${id}`);
  }

  //Admin endpoints

  // GET /api/reports
  adminGetAll(
    filter: ReportFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ReportDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReportDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/reports/{id}/resolve
  adminResolve(
    id: number,
    dto: AdminResolveReportDto
  ): Observable<ApiResponse<ReportDto>> {
    return this.http.post<ApiResponse<ReportDto>>(
      `${this.baseUrl}/${id}/resolve`,
      dto
    );
  }
}