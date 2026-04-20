import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
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
} from '../dtos/loanDTO';
import { LoanFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class LoanService {
  private readonly baseUrl = 'https://localhost:7183/api/loans';

  constructor(private http: HttpClient) {}

  //Borrower endpoints

  // POST /api/loans
  create(dto: CreateLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(this.baseUrl, dto);
  }

  // POST /api/loans/{id}/cancel
  cancel(id: number, dto: CancelLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/${id}/cancel`,
      dto
    );
  }

  // POST /api/loans/{id}/request-extension
  requestExtension(
    id: number,
    dto: RequestExtensionDto
  ): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/${id}/request-extension`,
      dto
    );
  }

  // POST /api/loans/pickup
  confirmPickup(dto: ScanQrCodeDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/pickup`,
      dto
    );
  }

  // POST /api/loans/return
  confirmReturn(dto: ScanQrCodeDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/return`,
      dto
    );
  }

  //Lender/Owner endpoints

  // POST /api/loans/{id}/decide
  decide(id: number, dto: OwnerDecideLoanDto): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/${id}/decide`,
      dto
    );
  }

  // POST /api/loans/{id}/decide-extension
  decideExtension(
    id: number,
    dto: DecideExtensionDto
  ): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/${id}/decide-extension`,
      dto
    );
  }

  //User queites

  // GET /api/loans/{id}
  getById(id: number): Observable<ApiResponse<LoanDto>> {
    return this.http.get<ApiResponse<LoanDto>>(`${this.baseUrl}/${id}`);
  }

  // GET /api/loans/my/borrowing
  getMyAsBorrower(
    filter: LoanFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/my/borrowing`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/loans/my/lending
  getMyAsLender(
    filter: LoanFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/my/lending`,
      { params: { ...filter, ...request } as any }
    );
  }

  //Admin queries

  // GET /api/loans/admin/all
  adminGetAll(
    filter: LoanFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<LoanListDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanListDto>>>(
      `${this.baseUrl}/admin/all`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/loans/admin/{id}
  adminGetById(id: number): Observable<ApiResponse<LoanDto>> {
    return this.http.get<ApiResponse<LoanDto>>(`${this.baseUrl}/admin/${id}`);
  }

  // GET /api/loans/admin/pending
  adminGetPending(): Observable<ApiResponse<AdminPendingLoanDto[]>> {
    return this.http.get<ApiResponse<AdminPendingLoanDto[]>>(
      `${this.baseUrl}/admin/pending`
    );
  }

  // GET /api/loans/admin/pending/count
  adminGetPendingCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(
      `${this.baseUrl}/admin/pending/count`
    );
  }

  // POST /api/loans/admin/{id}/review
  adminReview(
    id: number,
    dto: AdminReviewLoanDto
  ): Observable<ApiResponse<LoanDto>> {
    return this.http.post<ApiResponse<LoanDto>>(
      `${this.baseUrl}/admin/${id}/review`,
      dto
    );
  }
}