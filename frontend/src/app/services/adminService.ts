import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = 'https://localhost:7183/api/admin';

  constructor(private http: HttpClient) {}

  // ── Dashboard ─────────────────────────────────────────────────────────────

  getDashboard(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/dashboard`).pipe(map(r => r.data));
  }

  // ── Users ─────────────────────────────────────────────────────────────────

  getAllUsers(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/users`, { params }).pipe(map(r => r.data));
  }

  getUserById(userId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/users/${userId}`).pipe(map(r => r.data));
  }

  banUser(userId: string, dto: { reason: string; banExpiresAt?: string }): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/ban`, dto);
  }

  unbanUser(userId: string, dto: { reason?: string }): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/unban`, dto);
  }

  getBanHistory(userId: string, filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/users/${userId}/ban-history`, { params }).pipe(map(r => r.data));
  }

  getAllBans(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/bans`, { params }).pipe(map(r => r.data));
  }

  adjustUserScore(dto: { userId: string; pointsChanged: number; reason: string; loanId?: number; note?: string }): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/adjust-score`, dto);
  }

  // ── Items ─────────────────────────────────────────────────────────────────

  getAllItems(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/items`, { params }).pipe(map(r => r.data));
  }

  getItemById(itemId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/items/${itemId}`).pipe(map(r => r.data));
  }

  approveItem(itemId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/items/${itemId}/approve`, {});
  }

  rejectItem(itemId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/items/${itemId}/reject`, { reason });
  }

  deleteItem(itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/items/${itemId}`);
  }

  // ── Loans ─────────────────────────────────────────────────────────────────

  getAllLoans(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/loans`, { params }).pipe(map(r => r.data));
  }

  getLoanById(loanId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/loans/${loanId}`).pipe(map(r => r.data));
  }

  forceCancelLoan(loanId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/loans/${loanId}/force-cancel`, { reason });
  }

  // ── Disputes ──────────────────────────────────────────────────────────────

  getAllDisputes(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/disputes`, { params }).pipe(map(r => r.data));
  }

  getOpenDisputes(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/disputes/open`, { params }).pipe(map(r => r.data));
  }

  getDisputeStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/disputes/stats`).pipe(map(r => r.data));
  }

  getDisputeById(disputeId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/disputes/${disputeId}`).pipe(map(r => r.data));
  }

  resolveDispute(disputeId: number, dto: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/disputes/${disputeId}/resolve`, dto).pipe(map(r => r.data));
  }

  // ── Fines ─────────────────────────────────────────────────────────────────

  getAllFines(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/fines`, { params }).pipe(map(r => r.data));
  }

  getFineById(fineId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/fines/${fineId}`).pipe(map(r => r.data));
  }

  voidFine(fineId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/fines/${fineId}/void`, {});
  }

  approveFinePayment(fineId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/fines/${fineId}/approve-payment`, {}).pipe(map(r => r.data));
  }

  rejectFinePayment(fineId: number, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/fines/${fineId}/reject-payment`, { reason }).pipe(map(r => r.data));
  }

  // ── Appeals ───────────────────────────────────────────────────────────────

  getAllAppeals(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/appeals`, { params }).pipe(map(r => r.data));
  }

  getAppealById(appealId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/appeals/${appealId}`).pipe(map(r => r.data));
  }

  decideScoreAppeal(appealId: number, dto: { isApproved: boolean; adminNote?: string; newScore?: number }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appeals/${appealId}/decide/score`, dto).pipe(map(r => r.data));
  }

  decideFineAppeal(appealId: number, dto: { isApproved: boolean; adminNote?: string; resolution?: string; customFineAmount?: number }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appeals/${appealId}/decide/fine`, dto).pipe(map(r => r.data));
  }

  // ── Verifications ─────────────────────────────────────────────────────────

  getAllVerifications(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/verifications`, { params }).pipe(map(r => r.data));
  }

  getVerificationById(verificationId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/verifications/${verificationId}`).pipe(map(r => r.data));
  }

  approveVerification(verificationId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/verifications/${verificationId}/approve`, {}).pipe(map(r => r.data));
  }

  rejectVerification(verificationId: number, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/verifications/${verificationId}/reject`, { reason }).pipe(map(r => r.data));
  }

  // ── Reports ───────────────────────────────────────────────────────────────

  getAllReports(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/reports`, { params }).pipe(map(r => r.data));
  }

  getReportById(reportId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/reports/${reportId}`).pipe(map(r => r.data));
  }

  resolveReport(reportId: number, dto: { status: string; adminNote?: string }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/reports/${reportId}/resolve`, dto).pipe(map(r => r.data));
  }

  dismissReport(reportId: number, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/reports/${reportId}/dismiss`, { reason }).pipe(map(r => r.data));
  }

  // ── Support ───────────────────────────────────────────────────────────────

  getAllSupportThreads(filter?: any, request?: any): Observable<any> {
    const params = this.buildParams({ ...filter, ...request });
    return this.http.get<any>(`${this.baseUrl}/support`, { params }).pipe(map(r => r.data));
  }

  getSupportThreadById(threadId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/support/${threadId}`).pipe(map(r => r.data));
  }

  claimSupportThread(threadId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/support/${threadId}/claim`, {}).pipe(map(r => r.data));
  }

  closeSupportThread(threadId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/support/${threadId}/close`, {});
  }

  sendSupportMessage(threadId: number, content: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/support/${threadId}/message`, { content }).pipe(map(r => r.data));
  }

  // ── Notifications ─────────────────────────────────────────────────────────

  sendSystemNotification(userId: string, message: string, type: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/send`, { userId, message, type });
  }

  broadcastNotification(message: string, type: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/broadcast`, { message, type });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private buildParams(obj?: any): HttpParams {
    let params = new HttpParams();
    if (!obj) return params;
    Object.keys(obj).forEach(key => {
      const val = obj[key];
      if (val !== null && val !== undefined && val !== '') {
        params = params.set(key, val.toString());
      }
    });
    return params;
  }
}