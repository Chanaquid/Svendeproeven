import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminPendingLoanDto,
  AdminReviewLoanDto,
  CancelLoanDto,
  CreateLoanDto,
  DecideExtensionDto,
  LoanDto,
  LoanListDto,
  OwnerDecideLoanDto,
  RequestExtensionDto,
  ScanQrCodeDto,
} from '../dtos/loanDto';
import { LoanFilter } from '../dtos/filterDto';


@Injectable({
  providedIn: 'root',
})
export class LoanService {
  private readonly baseUrl = 'https://localhost:7183/api/loans';

  constructor(private http: HttpClient) {}

  // ── Param builder — never sends null / undefined values ──────────────────

  private buildParams(filter: LoanFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                  params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null)  params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.userId?.trim())           params = params.set('userId', filter.userId.trim());
    if (filter.borrowerId?.trim())       params = params.set('borrowerId', filter.borrowerId.trim());
    if (filter.lenderId?.trim())         params = params.set('lenderId', filter.lenderId.trim());
    if (filter.itemId != null)           params = params.set('itemId', filter.itemId.toString());
    if (filter.status)                   params = params.set('status', filter.status);
    if (filter.extensionRequestStatus)   params = params.set('extensionRequestStatus', filter.extensionRequestStatus);
    if (filter.isOverdue != null)        params = params.set('isOverdue', filter.isOverdue.toString());
    if (filter.createdAfter?.trim())     params = params.set('createdAfter', filter.createdAfter!.trim());
    if (filter.startsAfter?.trim())      params = params.set('startsAfter', filter.startsAfter!.trim());
    if (filter.endsBefore?.trim())       params = params.set('endsBefore', filter.endsBefore!.trim());
    if (filter.hasFines != null)         params = params.set('hasFines', filter.hasFines.toString());
    if (filter.hasDisputes != null)      params = params.set('hasDisputes', filter.hasDisputes.toString());
    if (filter.hasMessages != null)      params = params.set('hasMessages', filter.hasMessages.toString());
    if (filter.search?.trim())           params = params.set('search', filter.search.trim());

    return params;
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  getCompletedLoansCount(): Observable<number> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/completed/count`)
      .pipe(map(res => res.data ?? 0));
  }

  // ── Borrower endpoints ────────────────────────────────────────────────────

  // POST /api/loans
  create(dto: CreateLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(this.baseUrl, dto);
  }

  // POST /api/loans/{id}/cancel
  cancel(id: number, dto: CancelLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}/cancel`, dto);
  }

  // POST /api/loans/{id}/request-extension
  requestExtension(id: number, dto: RequestExtensionDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}/request-extension`, dto);
  }

  // POST /api/loans/pickup
  confirmPickup(dto: ScanQrCodeDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/pickup`, dto);
  }

  // POST /api/loans/return
  confirmReturn(dto: ScanQrCodeDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/return`, dto);
  }

  // ── Lender / Owner endpoints ──────────────────────────────────────────────

  // POST /api/loans/{id}/decide
  decide(id: number, dto: OwnerDecideLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}/decide`, dto);
  }

  // POST /api/loans/{id}/decide-extension
  decideExtension(id: number, dto: DecideExtensionDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}/decide-extension`, dto);
  }

  // ── User queries ──────────────────────────────────────────────────────────

  // GET /api/loans/{id}
  getById(id: number): Observable<ApiResponse<LoanDto>> {
    return this.http.get<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}`);
  }

  // GET /api/loans/my/borrowing
  getMyAsBorrower(filter: LoanFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/my/borrowing`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/loans/my/lending
  getMyAsLender(filter: LoanFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/my/lending`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/loans/by-item/{itemId}
  getMyLoanForItem(itemId: number): Observable<ApiResponse<LoanDto | null>> {
    return this.http.get<ApiResponse<LoanDto | null>>(`${this.baseUrl}/by-item/${itemId}`);
  }

  // ── Admin queries ─────────────────────────────────────────────────────────

  // GET /api/loans/admin/all
  adminGetAll(filter: LoanFilter, request: PagedRequest): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/admin/all`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/loans/admin/{id}
  adminGetById(id: number): Observable<ApiResponse<LoanDto>> {
    return this.http.get<ApiResponse<LoanDto>>(`${this.baseUrl}/admin/${id}`);
  }

  // GET /api/loans/admin/pending
  adminGetPending(): Observable<ApiResponse<AdminPendingLoanDto[]>> {
    return this.http.get<ApiResponse<AdminPendingLoanDto[]>>(`${this.baseUrl}/admin/pending`);
  }

  // GET /api/loans/admin/pending/count
  adminGetPendingCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/admin/pending/count`);
  }

  // POST /api/loans/admin/{id}/review
  adminReview(id: number, dto: AdminReviewLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(`${this.baseUrl}/admin/${id}/review`, dto);
  }
}