import { Injectable } from '@angular/core';
import { Observable, from, forkJoin, throwError } from 'rxjs';
import { switchMap, map } from 'rxjs/operators';
import { ItemService } from './itemService';
import { UploadImageService } from './uploadImageService';
import { ApiResponse } from '../dtos/apiResponseDto';
import { ItemDto } from '../dtos/itemDto';
import { ItemPhotoDto } from '../dtos/itemPhotoDto';

export interface UploadedPhoto {
  file: File;
  previewUrl: string; //local object URL for preview before upload
}

@Injectable({ providedIn: 'root' })
export class ItemPhotoService {
  constructor(
    private itemService: ItemService,
    private uploadService: UploadImageService
  ) {}

  uploadAndAdd(
    itemId: number,
    file: File,
    isPrimary = false,
    displayOrder?: number
  ): Observable<ApiResponse<ItemDto>> {
    return from(this.uploadService.uploadImage(file)).pipe(
      switchMap((url) =>
        this.itemService.addPhoto(itemId, { photoUrl: url, isPrimary, displayOrder })
      )
    );
  }


  uploadAndAddMany(
    itemId: number,
    files: File[],
    markFirstAsPrimary = false
  ): Observable<ApiResponse<ItemDto>[]> {
    if (files.length === 0) return throwError(() => new Error('No files provided'));

    const uploads$ = files.map((file, index) =>
      this.uploadAndAdd(itemId, file, markFirstAsPrimary && index === 0, index)
    );

    return forkJoin(uploads$);
  }


  deletePhoto(itemId: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.itemService.deletePhoto(itemId, photoId);
  }


  setPrimary(itemId: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.itemService.setPrimaryPhoto(itemId, photoId);
  }

  replacePhoto(
    itemId: number,
    oldPhotoId: number,
    newFile: File,
    isPrimary = false
  ): Observable<ApiResponse<ItemDto>> {
    return this.itemService.deletePhoto(itemId, oldPhotoId).pipe(
      switchMap(() => this.uploadAndAdd(itemId, newFile, isPrimary))
    );
  }

 
  createPreview(file: File): string {
    return URL.createObjectURL(file);
  }

  revokePreview(url: string): void {
    URL.revokeObjectURL(url);
  }


  validate(file: File, maxSizeMb = 4): string | null {
    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) return 'Only JPEG, PNG, and WebP images are allowed.';
    if (file.size > maxSizeMb * 1024 * 1024) return `Image must be under ${maxSizeMb}MB.`;
    return null;
  }
}