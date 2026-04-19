import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../dtos/apiResponseDto';
import { PagedRequest, PagedResult } from '../dtos/paginationDto';
import {
  DirectConversationDto,
  DirectConversationListDto,
  UnreadCountsDto,
} from '../dtos/directConversationDto';
import { DirectMessageDto, SendDirectMessageDto } from '../dtos/directMessageDto'
import { ConversationFilter, MessageFilter } from '../dtos/filterDto';

@Injectable({
  providedIn: 'root',
})
export class ConversationService {
  private readonly baseUrl = 'https://localhost:7183/api/conversations';

  constructor(private http: HttpClient) {}

  //Direct conversation endpoints

  // POST /api/conversations/with/{otherUserId}
  getOrCreate(otherUserId: string): Observable<ApiResponse<DirectConversationDto>> {
    return this.http.post<ApiResponse<DirectConversationDto>>(
      `${this.baseUrl}/with/${otherUserId}`,
      {}
    );
  }

  // GET /api/conversations
  getMyConversations(
    filter: ConversationFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DirectConversationListDto>>> {
    return this.http.get<ApiResponse<PagedResult<DirectConversationListDto>>>(
      this.baseUrl,
      { params: { ...filter, ...request } as any }
    );
  }

  // GET /api/conversations/{conversationId}
  getById(conversationId: number): Observable<ApiResponse<DirectConversationDto>> {
    return this.http.get<ApiResponse<DirectConversationDto>>(
      `${this.baseUrl}/${conversationId}`
    );
  }

  // DELETE /api/conversations/{conversationId}
  delete(conversationId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${this.baseUrl}/${conversationId}`
    );
  }

  // POST /api/conversations/{conversationId}/restore
  restore(conversationId: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/${conversationId}/restore`,
      {}
    );
  }

  //Direct message endpoints

  // POST /api/conversations/{conversationId}/messages
  sendMessage(
    conversationId: number,
    dto: SendDirectMessageDto
  ): Observable<ApiResponse<DirectMessageDto>> {
    return this.http.post<ApiResponse<DirectMessageDto>>(
      `${this.baseUrl}/${conversationId}/messages`,
      dto
    );
  }

  // GET /api/conversations/{conversationId}/messages
  getMessages(
    conversationId: number,
    filter: MessageFilter,
    request: PagedRequest
  ): Observable<ApiResponse<PagedResult<DirectMessageDto>>> {
    return this.http.get<ApiResponse<PagedResult<DirectMessageDto>>>(
      `${this.baseUrl}/${conversationId}/messages`,
      { params: { ...filter, ...request } as any }
    );
  }

  // POST /api/conversations/{conversationId}/read
  markAsRead(conversationId: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/${conversationId}/read`,
      {}
    );
  }

  //Unread counts

  // GET /api/conversations/unread-count
  getTotalUnreadCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/unread-count`);
  }

  // GET /api/conversations/unread-counts
  getUnreadCountsPerConversation(): Observable<ApiResponse<UnreadCountsDto>> {
    return this.http.get<ApiResponse<UnreadCountsDto>>(`${this.baseUrl}/unread-counts`);
  }
}