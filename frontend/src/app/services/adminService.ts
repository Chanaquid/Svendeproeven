import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminDashboardDto, ItemHistoryDto } from '../dtos/adminDTO';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = 'https://localhost:7183/api/admin';
  constructor(private http: HttpClient) {}

  getDashboard(): Observable<AdminDashboardDto> {
    return this.http.get<AdminDashboardDto>(`${this.baseUrl}/dashboard`);
  }

  getItemHistory(itemId: number): Observable<ItemHistoryDto> {
    return this.http.get<ItemHistoryDto>(`${this.baseUrl}/items/${itemId}/history`);
  }
}