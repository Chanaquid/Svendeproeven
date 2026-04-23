import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  AdminFineVerifyPaymentDto,
  CreateCustomFineDto,
  CreateLoanDisputeFineDto,
  FineDto,
  FineListDto,
  FineStatsDto,
  SubmitPaymentProofDto,
  UpdateFineDto,
} from '../dtos/fineDto';
import { FineFilter } from '../dtos/filterDto';
import { FineStatus } from '../dtos/enums';

@Injectable({
  providedIn: 'root',
})
export class FineService {
  private readonly baseUrl = 'https://localhost:7183/api/fines';

  constructor(private http: HttpClient) {}

  private buildParams(filter: FineFilter | null, request: PagedRequest): HttpParams {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.sortBy)                 params = params.set('sortBy', request.sortBy);
    if (request.sortDescending != null) params = params.set('sortDescending', request.sortDescending.toString());

    if (!filter) return params;

    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.status)                  params = params.set('status', filter.status);
    if (filter.type)                    params = params.set('type', filter.type);
    if (filter.userId)                  params = params.set('userId', filter.userId);
    if (filter.issuedByAdminId)         params = params.set('issuedByAdminId', filter.issuedByAdminId);
    if (filter.loanId != null)          params = params.set('loanId', filter.loanId.toString());
    if (filter.disputeId != null)       params = params.set('disputeId', filter.disputeId.toString());
    if (filter.minAmount != null)       params = params.set('minAmount', filter.minAmount.toString());
    if (filter.maxAmount != null)       params = params.set('maxAmount', filter.maxAmount.toString());
    if (filter.hasPaymentProof != null) params = params.set('hasPaymentProof', filter.hasPaymentProof.toString());
    if (filter.createdAfter)            params = params.set('createdAfter', filter.createdAfter);
    if (filter.paidAfter)               params = params.set('paidAfter', filter.paidAfter);

    return params;
  }



  //User endpoints

  // GET /api/fines/my
  getMyFines(filter: FineFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/my`,
      { params: this.buildParams(filter, request) }
    );
  }

  // GET /api/fines/my/{fineId}
  getMyFineById(fineId: number): Observable<ApiResponse<FineDto>> {
    return this.http.get<ApiResponse<FineDto>>(`${this.baseUrl}/my/${fineId}`);
  }

  // POST /api/fines/my/submit-proof
  submitPaymentProof(dto: SubmitPaymentProofDto): Observable<ApiResponse<FineDto>> {
    return this.http.post<ApiResponse<FineDto>>(
      `${this.baseUrl}/my/submit-proof`,
      dto
    );
  }

  //Admin endpoints

  // GET /api/fines
  adminGetAll(filter: FineFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      this.baseUrl,
      { params: this.buildParams(filter, request) }
    );
  }


  // GET /api/fines/{fineId}
  adminGetById(fineId: number): Observable<ApiResponse<FineDto>> {
    return this.http.get<ApiResponse<FineDto>>(`${this.baseUrl}/${fineId}`);
  }

  // GET /api/fines/by-status/{status}
  adminGetByStatus(status: FineStatus, filter: FineFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/by-status/${status}`,
      { params: this.buildParams(filter, request) }
    );
  }


  // GET /api/fines/pending-proof-review

  adminGetPendingProofReview(filter: FineFilter | null, request: PagedRequest): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/pending-proof-review`,
      { params: this.buildParams(filter, request) }
    );
  }
  // GET /api/fines/by-loan/{loanId}
  adminGetByLoan(loanId: number): Observable<ApiResponse<FineDto[]>> {
    return this.http.get<ApiResponse<FineDto[]>>(
      `${this.baseUrl}/by-loan/${loanId}`
    );
  }

  // GET /api/fines/by-dispute/{disputeId}
  adminGetByDispute(disputeId: number): Observable<ApiResponse<FineDto[]>> {
    return this.http.get<ApiResponse<FineDto[]>>(
      `${this.baseUrl}/by-dispute/${disputeId}`
    );
  }

  // GET /api/fines/stats
  adminGetStats(): Observable<ApiResponse<FineStatsDto>> {
    return this.http.get<ApiResponse<FineStatsDto>>(`${this.baseUrl}/stats`);
  }

  // POST /api/fines/issue/loan-dispute
  adminIssueLoanDisputeFine(dto: CreateLoanDisputeFineDto): Observable<ApiResponse<FineDto>> {
    return this.http.post<ApiResponse<FineDto>>(
      `${this.baseUrl}/issue/loan-dispute`,
      dto
    );
  }

  // POST /api/fines/issue/custom
  adminIssueCustomFine(dto: CreateCustomFineDto): Observable<ApiResponse<FineDto>> {
    return this.http.post<ApiResponse<FineDto>>(
      `${this.baseUrl}/issue/custom`,
      dto
    );
  }

  // PUT /api/fines/{fineId}
  adminUpdateFine(fineId: number, dto: UpdateFineDto): Observable<ApiResponse<FineDto>> {
    return this.http.put<ApiResponse<FineDto>>(`${this.baseUrl}/${fineId}`, dto);
  }

  // PUT /api/fines/{fineId}/void
  adminVoidFine(fineId: number): Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(
      `${this.baseUrl}/${fineId}/void`,
      {}
    );
  }

  // PUT /api/fines/{fineId}/verify-payment
  adminVerifyPayment(
    fineId: number,
    dto: AdminFineVerifyPaymentDto
  ): Observable<ApiResponse<FineDto>> {
    return this.http.put<ApiResponse<FineDto>>(
      `${this.baseUrl}/${fineId}/verify-payment`,
      dto
    );
  }
}