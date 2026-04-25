import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  DeleteAccountDto,
  UpdateAvatarDto,
  UpdateProfileDto,
  UserProfileDto,
  UserPublicProfileDto,
} from '../dtos/userDto';
import {
  AdminDeleteResultDto,
  AdminEditUserDto,
  AdminUserDto,
} from '../dtos/adminUserDto';
import { AdminAdjustScoreDto, ScoreHistoryDto } from '../dtos/scoreHistoryDto';
import { AppealDto } from '../dtos/appealDto';
import { DisputeListDto } from '../dtos/disputeDto';
import { FineListDto } from '../dtos/fineDto';
import { ItemListDto } from '../dtos/itemDto';
import { LoanListDto } from '../dtos/loanDto';
import { SupportThreadListDto } from '../dtos/supportThreadDto';
import { VerificationRequestDto } from '../dtos/verificationRequestDto';
import { BanUserDto, UnbanUserDto } from '../dtos/userBanHistoryDto';
import {
  AppealFilter,
  DisputeFilter,
  FineFilter,
  ItemFilter,
  LoanFilter,
  ScoreHistoryFilter,
  SupportThreadFilter,
  UserFilter,
  VerificationRequestFilter,
} from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly baseUrl = 'https://localhost:7183/api/users';
  private readonly adminBaseUrl = 'https://localhost:7183/api/admin/users';

  constructor(private http: HttpClient) {}

  // ── Helper ────────────────────────────────────────────────────────────────

  private buildParams(filter: any | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    // Map UserFilter specific fields
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.role) params = params.set('role', filter.role);
    if (filter.excludesAdmin != null) params = params.set('excludesAdmin', filter.excludesAdmin.toString());
    if (filter.includeDeleted != null) params = params.set('includeDeleted', filter.includeDeleted.toString());
    if (filter.isPermanentBan != null) params = params.set('isPermanentBan', filter.isPermanentBan.toString());
    if (filter.isBanned != null) params = params.set('isBanned', filter.isBanned.toString());
    if (filter.isDeleted != null) params = params.set('isDeleted', filter.isDeleted.toString());
    if (filter.isVerified != null) params = params.set('isVerified', filter.isVerified.toString());
    if (filter.hasUnpaidFines != null) params = params.set('hasUnpaidFines', filter.hasUnpaidFines.toString());
    if (filter.minScore != null) params = params.set('minScore', filter.minScore.toString());
    if (filter.maxScore != null) params = params.set('maxScore', filter.maxScore.toString());
    if (filter.latitude != null) params = params.set('latitude', filter.latitude.toString());
    if (filter.longitude != null) params = params.set('longitude', filter.longitude.toString());
    if (filter.radiusKm != null) params = params.set('radiusKm', filter.radiusKm.toString());

    // Support for other nested filters (Items, Loans, etc.) if passed to Admin detail sub-queries
    if (filter.status) params = params.set('status', filter.status);

    return params;
  }

  // ── User endpoints ────────────────────────────────────────────────────────

  getTotalUsersCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/totalUsers`);
  }

  getMyProfile(): Observable<ApiResponse<UserProfileDto>> {
    return this.http.get<ApiResponse<UserProfileDto>>(`${this.baseUrl}/me`);
  }

  getPublicProfile(userId: string): Observable<ApiResponse<UserPublicProfileDto>> {
    return this.http.get<ApiResponse<UserPublicProfileDto>>(`${this.baseUrl}/${userId}/public`);
  }

  updateProfile(dto: UpdateProfileDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.put<ApiResponse<UserProfileDto>>(`${this.baseUrl}/me`, dto);
  }

  updateAvatar(dto: UpdateAvatarDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.patch<ApiResponse<UserProfileDto>>(`${this.baseUrl}/me/avatar`, dto);
  }

  deleteAccount(dto: DeleteAccountDto): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/me`, { body: dto });
  }

  searchUsers(filter: UserFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<UserProfileDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserProfileDto>>>(`${this.baseUrl}/search`, {
      params: this.buildParams(filter, request),
    });
  }

  // ── Admin Endpoints ───────────────────────────────────────────────────────

  getUsers(filter: UserFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(this.adminBaseUrl, {
      params: this.buildParams(filter, request),
    });
  }

  getAllIncludingDeleted(filter: UserFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(`${this.adminBaseUrl}/all`, {
      params: this.buildParams(filter, request),
    });
  }

  getBannedUsers(filter: UserFilter, request: PagedRequest, tempBansOnly = false): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    let params = this.buildParams(filter, request);
    params = params.set('tempBansOnly', tempBansOnly.toString());
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(`${this.adminBaseUrl}/banned`, { params });
  }

  getUserById(userId: string): Observable<ApiResponse<AdminUserDto>> {
    return this.http.get<ApiResponse<AdminUserDto>>(`${this.adminBaseUrl}/${userId}`);
  }

  getUserItems(userId: string, filter: ItemFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(`${this.adminBaseUrl}/${userId}/items`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserLoans(userId: string, filter: LoanFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(`${this.adminBaseUrl}/${userId}/loans`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserFines(userId: string, filter: FineFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(`${this.adminBaseUrl}/${userId}/fines`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserScoreHistory(userId: string, filter: ScoreHistoryFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<ScoreHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<ScoreHistoryDto>>>(`${this.adminBaseUrl}/${userId}/score-history`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserAppeals(userId: string, filter: AppealFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(`${this.adminBaseUrl}/${userId}/appeals`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserReports(userId: string, request: PagedRequest): Observable<ApiResponse<PagedResult<any>>> {
    return this.http.get<ApiResponse<PagedResult<any>>>(
      `${this.adminBaseUrl}/${userId}/reports`,
      { params: { ...request } as any }
    );
  }

  getUserDisputes(userId: string, filter: DisputeFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(`${this.adminBaseUrl}/${userId}/disputes`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserVerifications(userId: string, filter: VerificationRequestFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(`${this.adminBaseUrl}/${userId}/verifications`, {
      params: this.buildParams(filter, request),
    });
  }

  getUserSupportThreads(userId: string, filter: SupportThreadFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<SupportThreadListDto>>> {
    return this.http.get<ApiResponse<PagedResult<SupportThreadListDto>>>(`${this.adminBaseUrl}/${userId}/support-threads`, {
      params: this.buildParams(filter, request),
    });
  }

  updateUser(userId: string, dto: AdminEditUserDto): Observable<ApiResponse<AdminUserDto>> {
    return this.http.put<ApiResponse<AdminUserDto>>(`${this.adminBaseUrl}/${userId}`, dto);
  }

  adjustScore(userId: string, dto: AdminAdjustScoreDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.adminBaseUrl}/${userId}/score`, dto);
  }

  banUser(userId: string, dto: BanUserDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.adminBaseUrl}/${userId}/ban`, dto);
  }

  unbanUser(userId: string, dto: UnbanUserDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.adminBaseUrl}/${userId}/unban`, dto);
  }

  deleteUser(userId: string, note?: string): Observable<ApiResponse<AdminDeleteResultDto>> {
    const params = note ? new HttpParams().set('note', note) : undefined;
    return this.http.delete<ApiResponse<AdminDeleteResultDto>>(`${this.adminBaseUrl}/${userId}`, { params });
  }
}