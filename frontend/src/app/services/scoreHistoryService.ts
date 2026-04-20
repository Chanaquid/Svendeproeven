import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminAdjustScoreDto,
  ScoreHistoryDto,
  UserScoreSummaryDto,
} from '../dtos/scoreHistoryDto';
import { ScoreHistoryFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ScoreHistoryService {
  private readonly baseUrl = 'https://localhost:7183/api/score-history';

  constructor(private http: HttpClient) {}

  //User

  // GET /api/score-history/my
  getMy(
    filter: ScoreHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ScoreHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<ScoreHistoryDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/score-history/my/summary
  getMySummary(): Observable<ApiResponse<UserScoreSummaryDto>> {
    return this.http.get<ApiResponse<UserScoreSummaryDto>>(
      `${this.baseUrl}/my/summary`
    );
  }

  //Admin

  // GET /api/score-history
  adminGetAll(
    filter: ScoreHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ScoreHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<ScoreHistoryDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/score-history/user/{userId}
  adminGetByUserId(
    userId: string,
    filter: ScoreHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ScoreHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<ScoreHistoryDto>>>(
      `${this.baseUrl}/user/${userId}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/score-history/user/{userId}/summary
  adminGetSummaryByUserId(
    userId: string
  ): Observable<ApiResponse<UserScoreSummaryDto>> {
    return this.http.get<ApiResponse<UserScoreSummaryDto>>(
      `${this.baseUrl}/user/${userId}/summary`
    );
  }

  // POST /api/score-history/adjust
  adminAdjust(dto: AdminAdjustScoreDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/adjust`,
      dto
    );
  }
}