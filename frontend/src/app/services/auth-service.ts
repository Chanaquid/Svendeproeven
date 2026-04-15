import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthDTO } from '../dtos/authDTO';
import { ApiResponseDTO } from '../dtos/apiResponseDTO';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly baseUrl = 'https://localhost:7183/api/auth';

    private _scheduleWarning?: () => void;
    setScheduler(fn: () => void): void { this._scheduleWarning = fn; }
 

  constructor(private http: HttpClient) {}


  register(dto: AuthDTO.RegisterDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/register`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }

  confirmEmail(userId: string, token: string): Observable<ApiResponseDTO> {
    return this.http.get<ApiResponseDTO>(`${this.baseUrl}/confirm-email`, {
      params: { userId, token },
    });
  }

  
  login(dto: AuthDTO.LoginDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/login`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }


  
  refresh(dto: AuthDTO.RefreshTokenDTO): Observable<AuthDTO.AuthResponseDTO> {
    return this.http
      .post<AuthDTO.AuthResponseDTO>(`${this.baseUrl}/refresh`, dto)
      .pipe(tap((res) => { this.saveTokens(res.token, res.refreshToken); this._scheduleWarning?.(); }));
  }

  
  logout(): Observable<ApiResponseDTO> {
    return this.http
      .post<ApiResponseDTO>(`${this.baseUrl}/logout`, {})
      .pipe(tap(() => this.clearTokens()));
  }

  
  changePassword(currentPassword: string, newPassword: string): Observable<ApiResponseDTO> {
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/change-password`, {
      currentPassword,
      newPassword,
    });
  }



  forgotPassword(email: string): Observable<ApiResponseDTO> {
    const dto: AuthDTO.ForgotPasswordDTO = { email };
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/forgot-password`, dto);
  }


  resetPassword(dto: AuthDTO.ResetPasswordDTO): Observable<ApiResponseDTO> {
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/reset-password`, dto);
  }

 
  
  resendConfirmation(email: string): Observable<ApiResponseDTO> {
    const dto: AuthDTO.ForgotPasswordDTO = { email };
    return this.http.post<ApiResponseDTO>(`${this.baseUrl}/resend-confirmation`, dto);
  }

  isAdmin(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin';
    } catch {
      return false;
    }
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
}