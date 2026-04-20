import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  DeleteAccountDto,
  UpdateAvatarDto,
  UpdateProfileDto,
  UserProfileDto,
  UserPublicProfileDto,
} from '../dtos/userDTO';
import {
  AdminDeleteResultDto,
  AdminEditUserDto,
  AdminUserDto,
} from '../dtos/adminUserDto';
import { AdminAdjustScoreDto, ScoreHistoryDto } from '../dtos/scoreHistoryDto';
import { AppealDto } from '../dtos/appealDTO';
import { DisputeListDto } from '../dtos/disputeDTO';
import { FineListDto } from '../dtos/fineDTO';
import { ItemListDto } from '../dtos/itemDTO';
import { LoanListDto } from '../dtos/loanDTO';
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
} from '../dtos/filterDto'

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly baseUrl = 'https://localhost:7183/api/users';
  private readonly adminBaseUrl = 'https://localhost:7183/api/admin/users';

  constructor(private http: HttpClient) {}

  //User endpoints

  // GET /api/users/totalUsers
  getTotalUsersCount(): Observable<ApiResponse<number>> {
  return this.http.get<ApiResponse<number>>(
    `${this.baseUrl}/totalUsers`
  );
}

  // GET /api/users/me
  getMyProfile(): Observable<ApiResponse<UserProfileDto>> {
    return this.http.get<ApiResponse<UserProfileDto>>(`${this.baseUrl}/me`);
  }

  // GET /api/users/{userId}/public
  getPublicProfile(userId: string): Observable<ApiResponse<UserPublicProfileDto>> {
    return this.http.get<ApiResponse<UserPublicProfileDto>>(
      `${this.baseUrl}/${userId}/public`
    );
  }

  // PUT /api/users/me
  updateProfile(dto: UpdateProfileDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.put<ApiResponse<UserProfileDto>>(`${this.baseUrl}/me`, dto);
  }

  // PATCH /api/users/me/avatar
  updateAvatar(dto: UpdateAvatarDto): Observable<ApiResponse<UserProfileDto>> {
    return this.http.patch<ApiResponse<UserProfileDto>>(
      `${this.baseUrl}/me/avatar`,
      dto
    );
  }

  // DELETE /api/users/me
  deleteAccount(dto: DeleteAccountDto): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/me`, {
      body: dto,
    });
  }

  // GET /api/users/search
  searchUsers(
    filter: UserFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<UserProfileDto>>> {
    return this.http.get<ApiResponse<PagedResult<UserProfileDto>>>(
      `${this.baseUrl}/search`,
      { params: { ...filter, ...request } as any }
    );
  }

  //Admin Endpoints

  // GET /api/admin/users
  getUsers(
    filter: UserFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(
      `${this.adminBaseUrl}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/all
  getAllIncludingDeleted(
    filter: UserFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(
      `${this.adminBaseUrl}/all`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/banned
  getBannedUsers(
    filter: UserFilter,
    request: PagedRequest,
    tempBansOnly = false
  ): Observable<ApiResponse<PagedResult<AdminUserDto>>> {
    return this.http.get<ApiResponse<PagedResult<AdminUserDto>>>(
      `${this.adminBaseUrl}/banned`,
      { params: { ...filter, ...request, tempBansOnly } as any }
    );
  }

  // GET /api/admin/users/{userId}
  getUserById(userId: string): Observable<ApiResponse<AdminUserDto>> {
    return this.http.get<ApiResponse<AdminUserDto>>(
      `${this.adminBaseUrl}/${userId}`
    );
  }

  // GET /api/admin/users/{userId}/items
  getUserItems(
    userId: string,
    filter: ItemFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ItemListDto>>> {
    return this.http.get<ApiResponse<PagedResult<ItemListDto>>>(
      `${this.adminBaseUrl}/${userId}/items`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/loans
  getUserLoans(
    userId: string,
    filter: LoanFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.adminBaseUrl}/${userId}/loans`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/fines
  getUserFines(
    userId: string,
    filter: FineFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.adminBaseUrl}/${userId}/fines`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/score-history
  getUserScoreHistory(
    userId: string,
    filter: ScoreHistoryFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<ScoreHistoryDto>>> {
    return this.http.get<ApiResponse<PagedResult<ScoreHistoryDto>>>(
      `${this.adminBaseUrl}/${userId}/score-history`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/appeals
  getUserAppeals(
    userId: string,
    filter: AppealFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<AppealDto>>> {
    return this.http.get<ApiResponse<PagedResult<AppealDto>>>(
      `${this.adminBaseUrl}/${userId}/appeals`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/disputes
  getUserDisputes(
    userId: string,
    filter: DisputeFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DisputeListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DisputeListDto>>>(
      `${this.adminBaseUrl}/${userId}/disputes`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/verifications
  getUserVerifications(
    userId: string,
    filter: VerificationRequestFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<VerificationRequestDto>>> {
    return this.http.get<ApiResponse<PagedResult<VerificationRequestDto>>>(
      `${this.adminBaseUrl}/${userId}/verifications`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/admin/users/{userId}/support-threads
  getUserSupportThreads(
    userId: string,
    filter: SupportThreadFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<SupportThreadListDto>>> {
    return this.http.get<ApiResponse<PagedResult<SupportThreadListDto>>>(
      `${this.adminBaseUrl}/${userId}/support-threads`,
      { params: { ...filter, ...request } as any }
    );
  }

  // PUT /api/admin/users/{userId}
  updateUser(
    userId: string,
    dto: AdminEditUserDto
  ): Observable<ApiResponse<AdminUserDto>> {
    return this.http.put<ApiResponse<AdminUserDto>>(
      `${this.adminBaseUrl}/${userId}`,
      dto
    );
  }

  // POST /api/admin/users/{userId}/score
  adjustScore(
    userId: string,
    dto: Omit<AdminAdjustScoreDto, 'userId'>
  ): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.adminBaseUrl}/${userId}/score`,
      dto
    );
  }

  // POST /api/admin/users/{userId}/ban
  banUser(userId: string, dto: BanUserDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.adminBaseUrl}/${userId}/ban`,
      dto
    );
  }

  // POST /api/admin/users/{userId}/unban
  unbanUser(userId: string, dto: UnbanUserDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.adminBaseUrl}/${userId}/unban`,
      dto
    );
  }

  // DELETE /api/admin/users/{userId}?note=
  deleteUser(
    userId: string,
    note?: string
  ): Observable<ApiResponse<AdminDeleteResultDto>> {
    return this.http.delete<ApiResponse<AdminDeleteResultDto>>(
      `${this.adminBaseUrl}/${userId}`,
      { params: note ? { note } : {} }
    );
  }
}