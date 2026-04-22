import { ChangeDetectorRef, Component } from '@angular/core';
import { UploadImageService } from '../../services/uploadImageService';
import { CommonModule } from '@angular/common';

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
    private cdr: ChangeDetectorRef
  ) {}

  // 📌 MULTI FILE SELECT
  onFileSelected(event: any) {
    this.selectedFiles = Array.from(event.target.files);
    this.errorMessage = null;
    this.uploadResponses = [];
  }

  // 📌 MULTI UPLOAD TEST
  async upload() {
    if (!this.selectedFiles.length) {
      this.errorMessage = "No files selected";
      return;
    }

    this.isUploading = true;
    this.uploadResponses = [];

    try {
      // 🚀 upload in parallel (fast)
      const uploadPromises = this.selectedFiles.map(file =>
        this.uploadService.uploadImage(file)
      );

      this.uploadResponses = await Promise.all(uploadPromises);

      console.log('All uploaded URLs:', this.uploadResponses);

    } catch (err) {
      this.errorMessage = "Upload failed";
    } finally {
      this.isUploading = false;
      this.cdr.detectChanges();
    }
  }
}