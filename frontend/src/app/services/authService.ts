import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import {
  AuthResponseDto,
  ChangePasswordDto,
  ForgotPasswordDto,
  LoginRequestDto,
  RefreshTokenRequestDto,
  ResendConfirmationDto,
  ResetPasswordDto,
} from '../dtos/authDto';
import {
  RegisterUserRequestDto,
  RegisterUserResponseDto,
} from '../dtos/userDto';
import { ApiResponse } from '../dtos/apiResponseDto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly baseUrl = 'https://localhost:7183/api/auth';

  private _scheduleWarning?: () => void;
  setScheduler(fn: () => void): void {
    this._scheduleWarning = fn;
  }

  constructor(private http: HttpClient) {}

  //POST /api/auth/register
  register(dto: RegisterUserRequestDto): Observable<ApiResponse<RegisterUserResponseDto>> {
    return this.http.post<ApiResponse<RegisterUserResponseDto>>(
      `${this.baseUrl}/register`,
      dto
    );
  }

  //GET /api/auth/confirm-email?userId=&token=
  confirmEmail(userId: string, token: string): Observable<ApiResponse<string>> {
    return this.http.get<ApiResponse<string>>(
      `${this.baseUrl}/confirm-email`,
      { params: { userId, token } }
    );
  }

  //POST /api/auth/login
  login(dto: LoginRequestDto): Observable<ApiResponse<AuthResponseDto>> {
    return this.http
      .post<ApiResponse<AuthResponseDto>>(`${this.baseUrl}/login`, dto)
      .pipe(
        tap((res) => {
          if (res.data) {
            this.saveTokens(res.data.token, res.data.refreshToken);
            this._scheduleWarning?.();
          }
        })
      );
  }

  //POST /api/auth/refresh
  refresh(dto: RefreshTokenRequestDto): Observable<ApiResponse<AuthResponseDto>> {
    return this.http
      .post<ApiResponse<AuthResponseDto>>(`${this.baseUrl}/refresh`, dto)
      .pipe(
        tap((res) => {
          if (res.data) {
            this.saveTokens(res.data.token, res.data.refreshToken);
            this._scheduleWarning?.();
          }
        })
      );
  }

  //POST /api/auth/logout  [Authorize]
  logout(): Observable<ApiResponse<string>> {
    return this.http
      .post<ApiResponse<string>>(`${this.baseUrl}/logout`, {})
      .pipe(tap(() => this.clearTokens()));
  }

  //POST /api/auth/change-password  [Authorize]
  changePassword(dto: ChangePasswordDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/change-password`,
      dto
    );
  }

  //POST /api/auth/forgot-password
  forgotPassword(dto: ForgotPasswordDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/forgot-password`,
      dto
    );
  }

  //POST /api/auth/reset-password
  resetPassword(dto: ResetPasswordDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/reset-password`,
      dto
    );
  }

  //POST /api/auth/resend-confirmation
  resendConfirmation(dto: ResendConfirmationDto): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/resend-confirmation`,
      dto
    );
  }

  //GET /api/auth/check-email?email=
  checkEmail(email: string): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(
      `${this.baseUrl}/check-email`,
      { params: { email } }
    );
  }

  //GET /api/auth/check-username?username=
  checkUsername(username: string): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(
      `${this.baseUrl}/check-username`,
      { params: { username } }
    );
  }

  // POST /api/auth/revoke-all  [Authorize]
  revokeAll(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/revoke-all`,
      {}
    );
  }


  //Token helpers

  saveTokens(token: string, refreshToken: string): void {
    localStorage.setItem('token', token);
    localStorage.setItem('refreshToken', refreshToken);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  clearTokens(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roles =
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ];
      return Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin';
    } catch {
      return false;
    }
  }


  getCurrentUserId(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['sub'] ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? null;
    } catch {
      return null;
    }
}

}