// src/app/services/upload.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UploadThingService {
  private readonly baseUrl = 'https://localhost:7183/api/uploadthing/avatar';

  constructor(private http: HttpClient) {}

  async uploadAvatar(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);

    const res = await firstValueFrom(
      this.http.post<{ url: string }>(`${this.baseUrl}`, formData)
    );

    return res.url;
  }
}