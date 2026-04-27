import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
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
export class VerificationRequest implements OnInit, OnChanges, OnDestroy {
  @Input() openVerificationId: number | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => {
    this.currentPage = 1;
    this.loadRequests();
  };

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',      label: 'Alle',     icon: '▤' },
    { id: 'pending',  label: 'Afventer', icon: '⏳', status: VerificationStatus.Pending },
    { id: 'approved', label: 'Godkendt', icon: '✓',  status: VerificationStatus.Approved },
    { id: 'rejected', label: 'Afvist',   icon: '✕',  status: VerificationStatus.Rejected },
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
    { value: 'newest', label: 'Nyeste først' },
    { value: 'oldest', label: 'Ældste først' },
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
    { value: VerificationDocumentType.Passport,       label: '🛂 Pas' },
    { value: VerificationDocumentType.NationalId,     label: '🪪 ID-kort' },
    { value: VerificationDocumentType.DrivingLicense, label: '🚗 Kørekort' },
  ];

  constructor(
    private verificationService: VerificationRequestService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

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

  ngOnInit(): void {
    this.loadRequests();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
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

  loadRequests(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find((t) => t.id === this.activeTab)?.status ?? null;

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'submittedAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    this.verificationService
      .getMyRequests({ search: this.searchQuery?.trim() || null, status }, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.listLoading = false;
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.requests = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find((t) => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Kunne ikke hente verifikationsanmodninger.';
          }
        },
        error: () => {
          this.listError = 'Der opstod en fejl. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = {
      page: 1,
      pageSize: 1,
      sortBy: 'submittedAt',
      sortDescending: true,
    };

    this.verificationService
      .getMyRequests(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const tab = this.tabs.find((t) => t.id === 'all');
          if (tab && res.data) {
            tab.count = res.data.totalCount;
            this.cdr.markForCheck();
          }
        },
      });

    const statusTabs: { id: TabId; status: VerificationStatus }[] = [
      { id: 'pending', status: VerificationStatus.Pending },
      { id: 'approved', status: VerificationStatus.Approved },
      { id: 'rejected', status: VerificationStatus.Rejected },
    ];

    for (const { id, status } of statusTabs) {
      this.verificationService
        .getMyRequests({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            const tab = this.tabs.find((t) => t.id === id);
            if (tab && res.data) {
              tab.count = res.data.totalCount;
              this.cdr.markForCheck();
            }
          },
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

    this.verificationService
      .getById(id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.detailLoading = false;
          this.isRefreshingDetail = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) this.selectedRequest = res.data;
        },
        error: (err) => {
          this.selectedId = null;
          this.listError = err.error?.message ?? 'Kunne ikke indlæse anmodning.';
          this.cdr.markForCheck();
        },
      });
  }

  // ─── Create form ──────────────────────────────────────────────────────────

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
      this.createError = 'Upload venligst et billede af dit dokument.';
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

      this.verificationService
        .submitRequest(dto)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => {
            this.isCreating = false;
            this.cdr.markForCheck();
          }),
        )
        .subscribe({
          next: (res) => {
            if (res.success && res.data) {
              this.createSuccess = 'Verifikationsanmodning indsendt! En admin gennemgår den snarest.';
              this.showCreateForm = false;
              this.resetCreateForm();
              this.loadRequests();
              this.loadTabCounts();
              setTimeout(() => {
                this.createSuccess = '';
                this.cdr.markForCheck();
              }, 5000);
            }
          },
          error: (err) => {
            this.createError = err.error?.message ?? 'Kunne ikke indsende anmodning.';
          },
        });
    } catch {
      this.uploadingDocument = false;
      this.isCreating = false;
      this.createError = 'Kunne ikke uploade dokumentbillede.';
      this.cdr.markForCheck();
    }
  }

  private resetCreateForm(): void {
    this.createDocumentType = VerificationDocumentType.Passport;
    this.createDocumentFile = null;
    this.createDocumentPreview = null;
    this.createError = '';
  }

  // ─── Tabs & filters ───────────────────────────────────────────────────────

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadRequests();
  }

  onSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadRequests();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadRequests();
  }

  trackById(_: number, r: VerificationRequestDto): number {
    return r.id;
  }
  openPhoto(url: string): void {
    this.selectedPhoto = url;
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  /** Maps status to one of the badge variants in styles.css */
  getStatusBadge(status: VerificationStatus | string): string {
    switch (status) {
      case VerificationStatus.Pending:  return 'badge badge-warning';
      case VerificationStatus.Approved: return 'badge badge-success';
      case VerificationStatus.Rejected: return 'badge badge-danger';
      default:                          return 'badge';
    }
  }

  getStatusLabel(status: VerificationStatus | string): string {
    switch (status) {
      case VerificationStatus.Pending:  return 'Afventer';
      case VerificationStatus.Approved: return 'Godkendt';
      case VerificationStatus.Rejected: return 'Afvist';
      default:                          return status as string;
    }
  }

  getDocumentTypeLabel(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return 'Pas';
      case VerificationDocumentType.NationalId:     return 'ID-kort';
      case VerificationDocumentType.DrivingLicense: return 'Kørekort';
      default:                                       return type as string;
    }
  }

  getDocumentTypeIcon(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return '🛂';
      case VerificationDocumentType.NationalId:     return '🪪';
      case VerificationDocumentType.DrivingLicense: return '🚗';
      default:                                       return '📄';
    }
  }
}
