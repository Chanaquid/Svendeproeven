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
  previewUrl: string; // local object URL for preview before upload
}

@Injectable({ providedIn: 'root' })
export class ItemPhotoService {
  constructor(
    private itemService: ItemService,
    private uploadService: UploadImageService
  ) {}

  /**
   * Upload a single file to Cloudinary, then attach it to the item.
   * isPrimary and displayOrder are optional.
   */
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

  /**
   * Upload multiple files sequentially and attach each to the item.
   * The first file is marked primary if markFirstAsPrimary is true.
   * Returns an array of ItemDto responses (one per photo added).
   *
   * Uses forkJoin so all uploads run in parallel — if any fail the
   * whole observable errors. Swap for a sequential approach if you
   * need strict ordering.
   */
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

  /**
   * Delete a photo from the item (no Cloudinary cleanup — that's handled
   * server-side or can be added later).
   */
  deletePhoto(itemId: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.itemService.deletePhoto(itemId, photoId);
  }

  /**
   * Set a photo as the primary/main photo for the item.
   */
  setPrimary(itemId: number, photoId: number): Observable<ApiResponse<ItemDto>> {
    return this.itemService.setPrimaryPhoto(itemId, photoId);
  }

  /**
   * Replace a photo: delete the old one, upload a new file, attach it.
   * Useful for "change photo" UX.
   */
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

  /**
   * Create a local preview URL for a File before uploading.
   * Remember to call URL.revokeObjectURL(url) when done to avoid memory leaks.
   */
  createPreview(file: File): string {
    return URL.createObjectURL(file);
  }

  revokePreview(url: string): void {
    URL.revokeObjectURL(url);
  }

  /**
   * Validate a file before upload. Returns an error string or null if valid.
   */
  validate(file: File, maxSizeMb = 4): string | null {
    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) return 'Only JPEG, PNG, and WebP images are allowed.';
    if (file.size > maxSizeMb * 1024 * 1024) return `Image must be under ${maxSizeMb}MB.`;
    return null;
  }
}