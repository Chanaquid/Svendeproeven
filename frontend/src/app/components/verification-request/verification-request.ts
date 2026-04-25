import {
  ChangeDetectorRef,
  Component,
  Input,
  OnDestroy,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize, takeUntil } from 'rxjs/operators';

import { VerificationRequestService } from '../../services/verificationRequestService';
import { UploadImageService } from '../../services/uploadImageService';
import {
  VerificationRequestDto,
  CreateVerificationRequestDto,
} from '../../dtos/verificationRequestDto';
import { VerificationDocumentType, VerificationStatus } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabId = 'all' | 'pending' | 'approved' | 'rejected';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  status?: VerificationStatus;
  count?: number;
}

@Component({
  selector: 'app-verification-request',
  imports: [CommonModule, FormsModule],
  templateUrl: './verification-request.html',
  styleUrl: './verification-request.css',
})
export class VerificationRequest implements OnInit, OnDestroy {

  @Input() openVerificationId: number | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => {
    this.currentPage = 1;
    this.loadRequests();
  };

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',      label: 'All',      icon: '▤' },
    { id: 'pending',  label: 'Pending',  icon: '⏳', status: VerificationStatus.Pending },
    { id: 'approved', label: 'Approved', icon: '✓',  status: VerificationStatus.Approved },
    { id: 'rejected', label: 'Rejected', icon: '✕',  status: VerificationStatus.Rejected },
  ];
  activeTab: TabId = 'all';

  // List state
  requests: VerificationRequestDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  totalCount = 0;
  searchQuery = '';
  sortFilter = 'newest';

  sortOptions = [
    { value: 'newest', label: 'Newest first' },
    { value: 'oldest', label: 'Oldest first' },
  ];

  // Detail state
  selectedId: number | null = null;
  selectedRequest: VerificationRequestDto | null = null;
  detailLoading = false;
  isRefreshingDetail = false;

  // Photo lightbox
  selectedPhoto: string | null = null;

  // Create form
  showCreateForm = false;
  createDocumentType: VerificationDocumentType = VerificationDocumentType.Passport;
  createDocumentFile: File | null = null;
  createDocumentPreview: string | null = null;
  isCreating = false;
  uploadingDocument = false;
  createError = '';
  createSuccess = '';

  readonly VerificationStatus = VerificationStatus;
  readonly VerificationDocumentType = VerificationDocumentType;

  documentTypeOptions = [
    { value: VerificationDocumentType.Passport,       label: '🛂 Passport' },
    { value: VerificationDocumentType.NationalId,     label: '🪪 National ID' },
    { value: VerificationDocumentType.DrivingLicense, label: '🚗 Driving License' },
  ];

  constructor(
    private verificationService: VerificationRequestService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  // ─── Dynamic page size ────────────────────────────────────────────────────────

  get pageSize(): number {
    const availableHeight = window.innerHeight - 64 - 52 - 48 - 80;
    return Math.max(5, Math.floor(availableHeight / 100));
  }

  get totalPages(): number {
    return getTotalPages(this.totalCount, this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadRequests();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadRequests();
    });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['openVerificationId'] && this.openVerificationId) {
      this.openRequest(this.openVerificationId);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────────

  loadRequests(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find(t => t.id === this.activeTab)?.status ?? null;

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'submittedAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    this.verificationService.getMyRequests(
      { search: this.searchQuery?.trim() || null, status },
      request
    )
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.requests = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find(t => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load verification requests.';
          }
        },
        error: () => { this.listError = 'An error occurred. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = {
      page: 1,
      pageSize: 1,
      sortBy: 'submittedAt',
      sortDescending: true,
    };

    // All tab — pass null so no filter params are sent
    this.verificationService.getMyRequests(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const tab = this.tabs.find(t => t.id === 'all');
          if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
        }
      });

    const statusTabs: { id: TabId; status: VerificationStatus }[] = [
      { id: 'pending',  status: VerificationStatus.Pending },
      { id: 'approved', status: VerificationStatus.Approved },
      { id: 'rejected', status: VerificationStatus.Rejected },
    ];

    for (const { id, status } of statusTabs) {
      this.verificationService.getMyRequests({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            const tab = this.tabs.find(t => t.id === id);
            if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
          }
        });
    }
  }

  openRequest(id: number): void {
    if (this.selectedId === id) return;

    const isFirstOpen = this.selectedRequest === null;
    this.selectedId = id;

    if (isFirstOpen) {
      this.detailLoading = true;
    } else {
      this.isRefreshingDetail = true;
    }

    this.verificationService.getById(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.detailLoading = false;
        this.isRefreshingDetail = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedRequest = res.data;
          }
        },
        error: (err) => {
          this.selectedId = null;
          this.listError = err.error?.message ?? 'Failed to load request.';
          this.cdr.markForCheck();
        },
      });
  }

  // ─── Create form ──────────────────────────────────────────────────────────────

  onDocumentFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.createDocumentFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.createDocumentPreview = e.target!.result as string;
      this.cdr.markForCheck();
    };
    reader.readAsDataURL(file);
    (event.target as HTMLInputElement).value = '';
  }

  async submitRequest(): Promise<void> {
    if (!this.createDocumentFile) {
      this.createError = 'Please upload your document image.';
      return;
    }

    this.isCreating = true;
    this.uploadingDocument = true;
    this.createError = '';

    try {
      const url = await this.uploadService.uploadImage(this.createDocumentFile);
      this.uploadingDocument = false;

      const dto: CreateVerificationRequestDto = {
        documentType: this.createDocumentType,
        documentUrl: url,
      };

      this.verificationService.submitRequest(dto)
        .pipe(takeUntil(this.destroy$), finalize(() => {
          this.isCreating = false;
          this.cdr.markForCheck();
        }))
        .subscribe({
          next: (res) => {
            if (res.success && res.data) {
              this.createSuccess = 'Verification request submitted! An admin will review it shortly.';
              this.showCreateForm = false;
              this.resetCreateForm();
              this.loadRequests();
              this.loadTabCounts();
              setTimeout(() => { this.createSuccess = ''; this.cdr.markForCheck(); }, 5000);
            }
          },
          error: (err) => {
            this.createError = err.error?.message ?? 'Failed to submit request.';
          },
        });
    } catch {
      this.uploadingDocument = false;
      this.isCreating = false;
      this.createError = 'Failed to upload document image.';
      this.cdr.markForCheck();
    }
  }

  private resetCreateForm(): void {
    this.createDocumentType = VerificationDocumentType.Passport;
    this.createDocumentFile = null;
    this.createDocumentPreview = null;
    this.createError = '';
  }

  // ─── Tabs & filters ───────────────────────────────────────────────────────────

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadRequests();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadRequests();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadRequests();
  }

  trackById(_: number, r: VerificationRequestDto): number { return r.id; }
  openPhoto(url: string): void { this.selectedPhoto = url; }

  // ─── UI Helpers ───────────────────────────────────────────────────────────────

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }

  getStatusClass(status: VerificationStatus | string): string {
    switch (status) {
      case VerificationStatus.Pending:  return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case VerificationStatus.Approved: return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case VerificationStatus.Rejected: return 'bg-red-400/10 text-red-400 border-red-400/20';
      default:                          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getDocumentTypeLabel(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return 'Passport';
      case VerificationDocumentType.NationalId:     return 'National ID';
      case VerificationDocumentType.DrivingLicense: return 'Driving License';
      default:                                      return type as string;
    }
  }

  getDocumentTypeIcon(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return '🛂';
      case VerificationDocumentType.NationalId:     return '🪪';
      case VerificationDocumentType.DrivingLicense: return '🚗';
      default:                                      return '📄';
    }
  }
}