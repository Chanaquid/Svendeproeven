import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

type UploadResponse = {
  url: string;
};

@Injectable({ providedIn: 'root' })
export class UploadImageService {
  private readonly baseUrl = 'https://localhost:7183/api/upload/image';

  constructor(private http: HttpClient) {}

  async uploadAvatar(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);

    const res = await firstValueFrom(
      this.http.post<{ url: string }>(this.baseUrl, formData)
    );

    return res.url;
  }


  async uploadImage(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);

    const res = await firstValueFrom(
      this.http.post<{ url: string }>(this.baseUrl, formData)
    );

    return res.url;
  }


}