import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UploadImageService } from '../../services/uploadImageService';

@Component({
  selector: 'app-test-upload',
  imports: [CommonModule],
  templateUrl: './test-upload.html',
  styleUrl: './test-upload.css',
})
export class TestUpload {
  selectedFiles: File[] = [];
  uploadResponses: string[] = [];
  errorMessage: string | null = null;
  isUploading = false;

  constructor(
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
  ) {}

  onFileSelected(event: any) {
    this.selectedFiles = Array.from(event.target.files);
    this.errorMessage = null;
    this.uploadResponses = [];
  }

  async upload() {
    if (!this.selectedFiles.length) {
      this.errorMessage = 'Ingen filer valgt';
      return;
    }

    this.isUploading = true;
    this.uploadResponses = [];

    try {
      const uploadPromises = this.selectedFiles.map((file) =>
        this.uploadService.uploadImage(file),
      );
      this.uploadResponses = await Promise.all(uploadPromises);
    } catch (err) {
      this.errorMessage = 'Upload mislykkedes';
    } finally {
      this.isUploading = false;
      this.cdr.detectChanges();
    }
  }
}
