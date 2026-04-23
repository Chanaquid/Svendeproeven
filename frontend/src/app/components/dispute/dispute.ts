import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { AuthService } from '../../services/authService';
import { DisputeService } from '../../services/disputeService';
import { UploadImageService } from '../../services/uploadImageService';

import { DisputeDto, DisputeListDto, AdminResolveDisputeDto, SubmitDisputeResponseDto, EditDisputeDto } from '../../dtos/disputeDto';
import { AddDisputePhotoDto } from '../../dtos/disputePhotoDto';
import { DisputeStatus, DisputeFiledAs, DisputeVerdict } from '../../dtos/enums';
import { DisputeFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';

type TabId = 'all' | 'awaiting' | 'pending' | 'overdue' | 'resolved' | 'cancelled';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  status?: DisputeStatus;
  count?: number;
}

@Component({
  selector: 'app-dispute',
  imports: [CommonModule, FormsModule],
  templateUrl: './dispute.html',
  styleUrl: './dispute.css',
})
export class Dispute implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  isAdmin = false;
  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',       label: 'All',              icon: '▤' },
    { id: 'awaiting',  label: 'Awaiting Response', icon: '⏳', status: DisputeStatus.AwaitingResponse },
    { id: 'pending',   label: 'Under Review',      icon: '🔍', status: DisputeStatus.PendingAdminReview },
    { id: 'overdue',   label: 'Overdue',           icon: '⚠️', status: DisputeStatus.PastDeadline },
    { id: 'resolved',  label: 'Resolved',          icon: '✓',  status: DisputeStatus.Resolved },
    { id: 'cancelled', label: 'Cancelled',         icon: '✕',  status: DisputeStatus.Cancelled },
  ];
  activeTab: TabId = 'all';

  // List state
  disputes: DisputeListDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  searchQuery = '';
  sortFilter = 'newest';

  // Detail state
  selectedId: number | null = null;
  selectedDispute: DisputeDto | null = null;
  detailLoading = false;

  // Photo lightbox
  selectedPhoto: string | null = null;

  // Edit claim
  editMode = false;
  editDescription = '';
  editError = '';
  isSavingEdit = false;

  // Submit response
  responseText = '';
  responseError = '';
  isSubmittingResponse = false;
  responsePhotoFiles: File[] = [];
  responsePhotoPreviews: string[] = [];
  responsePhotoError = '';

  // Evidence photo
  evidenceFile: File | null = null;
  evidencePreview: string | null = null;
  evidenceCaption = '';
  uploadingEvidence = false;
  evidenceError = '';

  // Admin resolve
  resolveVerdict = '';
  resolveNote = '';
  ownerFine: number | null = null;
  ownerScore: number | null = null;
  borrowerFine: number | null = null;
  borrowerScore: number | null = null;
  resolveError = '';
  isResolving = false;

  // Cancel
  showCancelConfirm = false;
  isCancelling = false;

  constructor(
    private authService: AuthService,
    private disputeService: DisputeService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.loadDisputes();
    this.loadTabCounts();

    // Debounce search to avoid flashing on every keystroke
    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadDisputes();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ────────────────────────────────────────────────────────────────────

  loadDisputes(): void {
    this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find(t => t.id === this.activeTab)?.status ?? null;

    const filter: DisputeFilter = {
      search: this.searchQuery?.trim() || null,
      status: status,
    };


    let sortBy: string;
    let sortDescending: boolean;

    switch (this.sortFilter) {
      case 'oldest':
        sortBy = 'createdAt';
        sortDescending = false;
        break;
      case 'az':
        sortBy = 'itemTitle';
        sortDescending = false;
        break;
      case 'za':
        sortBy = 'itemTitle';
        sortDescending = true;
        break;
      case 'newest':
      default:
        sortBy = 'createdAt';
        sortDescending = true;
        break;
    }


    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortFilter === 'az' || this.sortFilter === 'za' ? 'itemTitle' : 'createdAt',
      sortDescending: this.sortFilter === 'newest' || this.sortFilter === 'za',
    };

    this.disputeService.getMyAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.disputes = res.data.items;
            this.totalPages = res.data.totalPages;
            const tab = this.tabs.find(t => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load disputes.';
          }
        },
        error: () => { this.listError = 'An error occurred. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.disputeService.getMyAll(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => {
        const tab = this.tabs.find(t => t.id === 'all');
        if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
      }});

    const statusTabs: { id: TabId; status: DisputeStatus }[] = [
      { id: 'awaiting',  status: DisputeStatus.AwaitingResponse },
      { id: 'pending',   status: DisputeStatus.PendingAdminReview },
      { id: 'overdue',   status: DisputeStatus.PastDeadline },
      { id: 'resolved',  status: DisputeStatus.Resolved },
      { id: 'cancelled', status: DisputeStatus.Cancelled },
    ];

    for (const { id, status } of statusTabs) {
      this.disputeService.getMyAll({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({ next: (res) => {
          const tab = this.tabs.find(t => t.id === id);
          if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
        }});
    }
  }

  openDispute(id: number): void {
    if (this.selectedId === id) return;
    this.selectedId = id;
    this.selectedDispute = null;
    this.detailLoading = true;
    this.resetDetailForms();

    this.disputeService.getById(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.detailLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedDispute = res.data;
            this.editDescription = res.data.description;
            this.disputeService.markViewed(id).pipe(takeUntil(this.destroy$)).subscribe();
          }
        },
        error: (err) => {
          this.selectedId = null;
          this.listError = err.error?.message ?? 'Failed to load dispute.';
          this.cdr.markForCheck();
        },
      });
  }

  private resetDetailForms(): void {
    this.editMode = false;
    this.editError = '';
    this.responseText = '';
    this.responseError = '';
    this.responsePhotoFiles = [];
    this.responsePhotoPreviews = [];
    this.responsePhotoError = '';
    this.evidenceFile = null;
    this.evidencePreview = null;
    this.evidenceCaption = '';
    this.evidenceError = '';
    this.resolveVerdict = '';
    this.resolveNote = '';
    this.ownerFine = null;
    this.ownerScore = null;
    this.borrowerFine = null;
    this.borrowerScore = null;
    this.resolveError = '';
  }

  // ─── Tabs & filters ──────────────────────────────────────────────────────────

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadDisputes();
  }

  onSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadDisputes();
  }

  goToPage(p: number): void {
    this.currentPage = p;
    this.loadDisputes();
  }

  trackById(_: number, d: DisputeListDto): number { return d.id; }

  // ─── Permission checks ───────────────────────────────────────────────────────

  canSubmitResponse(): boolean {
    const d = this.selectedDispute;
    if (!d) return false;
    return d.status === DisputeStatus.AwaitingResponse && !d.isMine && !d.responseDescription;
  }

  canAddEvidence(): boolean {
    const d = this.selectedDispute;
    if (!d) return false;
    return d.isMine && (d.status === DisputeStatus.AwaitingResponse || d.status === DisputeStatus.PendingAdminReview);
  }

  canEdit(): boolean {
    const d = this.selectedDispute;
    return !!d && d.isMine && d.status === DisputeStatus.AwaitingResponse;
  }

  canCancel(): boolean {
    const d = this.selectedDispute;
    return !!d && d.isMine && d.status === DisputeStatus.AwaitingResponse;
  }

  canAdminResolve(): boolean {
    const d = this.selectedDispute;
    return !!d && (d.status === DisputeStatus.PendingAdminReview || d.status === DisputeStatus.PastDeadline);
  }

  // ─── Actions ─────────────────────────────────────────────────────────────────

  toggleEditMode(): void {
    this.editMode = !this.editMode;
    if (this.editMode && this.selectedDispute) {
      this.editDescription = this.selectedDispute.description;
    }
    this.editError = '';
  }

  saveEdit(): void {
    if (!this.selectedDispute || !this.editDescription.trim()) return;

    if (this.editDescription.trim().length < 20) {
      this.editError = 'Description must be at least 20 characters.';
      return;
    }

    this.isSavingEdit = true;
    this.editError = '';

    const dto: EditDisputeDto = { description: this.editDescription.trim() };

    this.disputeService.editDispute(this.selectedDispute.id, dto)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isSavingEdit = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedDispute = res.data;
            this.editMode = false;
            this.refreshListItem(res.data);
          }
        },
        error: (err) => { this.editError = err.error?.message ?? 'Failed to save.'; },
      });
  }

  get editDescriptionTooShort(): boolean {
    return this.editDescription.trim().length > 0 && this.editDescription.trim().length < 20;
  }

  submitResponse(): void {
    if (!this.selectedDispute || !this.responseText.trim()) return;

    if (this.responseText.trim().length < 20) {
      this.responseError = 'Response must be at least 20 characters.';
      return;
    }

    this.isSubmittingResponse = true;
    this.responseError = '';
    this.responsePhotoError = '';

    const dto: SubmitDisputeResponseDto = { responseDescription: this.responseText.trim() };

    this.disputeService.submitResponse(this.selectedDispute.id, dto)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isSubmittingResponse = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: async (res) => {
          if (res.success && res.data) {
            this.selectedDispute = res.data;
            this.responseText = '';
            if (this.responsePhotoFiles.length) {
              await this.uploadResponsePhotos(res.data.id);
            }
            this.loadDisputes();
          }
        },
        error: (err) => {
          this.responseError = err.error?.message ?? 'Failed to submit response.';
        },
      });
  }

  get responseTextTooShort(): boolean {
    return this.responseText.trim().length > 0 && this.responseText.trim().length < 20;
  }

  private async uploadResponsePhotos(disputeId: number): Promise<void> {
    for (const file of this.responsePhotoFiles) {
      try {
        const url = await this.uploadService.uploadImage(file);
        const dto: AddDisputePhotoDto = { photoUrl: url };
        await this.disputeService.addResponsePhoto(disputeId, dto)
          .pipe(takeUntil(this.destroy$))
          .toPromise();
      } catch (err: any) {
        this.responsePhotoError = err?.error?.message ?? 'Failed to upload one or more photos.';
        this.cdr.markForCheck();
        break;
      }
    }

    this.responsePhotoFiles = [];
    this.responsePhotoPreviews = [];

    if (!this.responsePhotoError) {
      this.openDispute(disputeId);
    } else {
      this.cdr.markForCheck();
    }
  }

  onResponsePhotosSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files || []);
    files.forEach(file => {
      this.responsePhotoFiles.push(file);
      const reader = new FileReader();
      reader.onload = (e) => {
        this.responsePhotoPreviews.push(e.target!.result as string);
        this.cdr.markForCheck();
      };
      reader.readAsDataURL(file);
    });
  }

  removeResponsePhoto(i: number): void {
    this.responsePhotoFiles.splice(i, 1);
    this.responsePhotoPreviews.splice(i, 1);
  }

  onEvidenceFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.evidenceFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.evidencePreview = e.target!.result as string;
      this.cdr.markForCheck();
    };
    reader.readAsDataURL(file);
  }

  async uploadEvidencePhoto(): Promise<void> {
    if (!this.evidenceFile || !this.selectedDispute) return;
    this.uploadingEvidence = true;
    this.evidenceError = '';

    try {
      const url = await this.uploadService.uploadImage(this.evidenceFile);
      const dto: AddDisputePhotoDto = { photoUrl: url, caption: this.evidenceCaption.trim() || undefined };
      this.disputeService.addFiledByPhoto(this.selectedDispute.id, dto)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.evidenceFile = null;
            this.evidencePreview = null;
            this.evidenceCaption = '';
            this.evidenceError = '';
            this.openDispute(this.selectedDispute!.id);
          },
          error: (err) => {
            this.evidenceError = err.error?.message ?? 'Failed to upload photo.';
            this.cdr.markForCheck();
          },
        });
    } catch {
      this.evidenceError = 'Failed to upload image.';
    } finally {
      this.uploadingEvidence = false;
      this.cdr.markForCheck();
    }
  }

  adminResolve(): void {
    if (!this.selectedDispute || !this.resolveVerdict) return;
    this.isResolving = true;
    this.resolveError = '';

    const dto: AdminResolveDisputeDto = {
      verdict: this.resolveVerdict as DisputeVerdict,
      adminNote: this.resolveNote.trim() || null,
      ownerPenalty: (this.ownerFine || this.ownerScore) ? { fineAmount: this.ownerFine, scoreAdjustment: this.ownerScore } : null,
      borrowerPenalty: (this.borrowerFine || this.borrowerScore) ? { fineAmount: this.borrowerFine, scoreAdjustment: this.borrowerScore } : null,
    };

    this.disputeService.adminResolve(this.selectedDispute.id, dto)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isResolving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedDispute = res.data;
            this.resolveVerdict = '';
            this.resolveNote = '';
            this.ownerFine = null;
            this.ownerScore = null;
            this.borrowerFine = null;
            this.borrowerScore = null;
            this.loadDisputes();
          }
        },
        error: (err) => { this.resolveError = err.error?.message ?? 'Failed to resolve dispute.'; },
      });
  }

  cancelDispute(): void {
    if (!this.selectedDispute) return;
    this.isCancelling = true;

    this.disputeService.cancelDispute(this.selectedDispute.id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isCancelling = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.showCancelConfirm = false;
            this.selectedId = null;
            this.selectedDispute = null;
            this.loadDisputes();
          }
        },
      });
  }

  private refreshListItem(dispute: DisputeDto): void {
    const idx = this.disputes.findIndex(d => d.id === dispute.id);
    if (idx !== -1) {
      this.disputes[idx] = { ...this.disputes[idx], status: dispute.status };
    }
  }

  // ─── UI helpers ──────────────────────────────────────────────────────────────

  openPhoto(url: string): void { this.selectedPhoto = url; }

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case DisputeStatus.AwaitingResponse:   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case DisputeStatus.PendingAdminReview: return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case DisputeStatus.Resolved:           return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case DisputeStatus.PastDeadline:       return 'bg-red-400/10 text-red-400 border-red-400/20';
      case DisputeStatus.Cancelled:          return 'bg-zinc-700 text-zinc-400 border-zinc-700';
      default:                               return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getVerdictClass(verdict: string): string {
    switch (verdict) {
      case DisputeVerdict.NoPenalty:         return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case DisputeVerdict.OwnerPenalized:    return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case DisputeVerdict.BorrowerPenalized: return 'bg-red-400/10 text-red-400 border-red-400/20';
      case DisputeVerdict.BothPenalized:     return 'bg-purple-400/10 text-purple-400 border-purple-400/20';
      default:                               return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}