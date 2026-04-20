import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDTO';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  LoanMessageDto,
  LoanUnreadCountDto,
  MarkLoanMessagesReadDto,
  SendLoanMessageDto,
} from '../dtos/loanMessageDto';

@Injectable({
  providedIn: 'root',
})
export class LoanMessageService {
  private readonly baseUrl = 'https://localhost:7183/api/loans';

  constructor(private http: HttpClient) {}

  // GET /api/loans/{loanId}/messages
  getMessages(
    loanId: number,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<LoanMessageDto>>> {
    return this.http.get<ApiResponse<PagedResult<LoanMessageDto>>>(
      `${this.baseUrl}/${loanId}/messages`,
      { params: { ...request } as any }
    );
  }

  // POST /api/loans/{loanId}/messages
  sendMessage(
    loanId: number,
    dto: SendLoanMessageDto
  ): Observable<ApiResponse<LoanMessageDto>> {
    return this.http.post<ApiResponse<LoanMessageDto>>(
      `${this.baseUrl}/${loanId}/messages`,
      dto
    );
  }

  // PATCH /api/loans/{loanId}/messages/read
  markAsRead(
    loanId: number,
    dto: MarkLoanMessagesReadDto
  ): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${this.baseUrl}/${loanId}/messages/read`,
      dto
    );
  }

  // GET /api/loans/{loanId}/messages/unread
  getUnreadCount(loanId: number): Observable<ApiResponse<LoanUnreadCountDto>> {
    return this.http.get<ApiResponse<LoanUnreadCountDto>>(
      `${this.baseUrl}/${loanId}/messages/unread`
    );
  }
}