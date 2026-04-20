import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
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
} from '../dtos/fineDTO';
import { FineFilter } from '../dtos/filterDto';
import { FineStatus } from '../dtos/enums';

@Injectable({
  providedIn: 'root',
})
export class FineService {
  private readonly baseUrl = 'https://localhost:7183/api/fines';

  constructor(private http: HttpClient) {}

  //User endpoints

  // GET /api/fines/my
  getMyFines(
    filter: FineFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/my`,
      { params: { ...filter, ...request } as any }
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
  adminGetAll(
    filter: FineFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/fines/{fineId}
  adminGetById(fineId: number): Observable<ApiResponse<FineDto>> {
    return this.http.get<ApiResponse<FineDto>>(`${this.baseUrl}/${fineId}`);
  }

  // GET /api/fines/by-status/{status}
  adminGetByStatus(
    status: FineStatus,
    filter: FineFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/by-status/${status}`,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/fines/pending-proof-review
  adminGetPendingProofReview(
    filter: FineFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<FineListDto>>> {
    return this.http.get<ApiResponse<PagedResult<FineListDto>>>(
      `${this.baseUrl}/pending-proof-review`,
      { params: { ...filter, ...request } as any }
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