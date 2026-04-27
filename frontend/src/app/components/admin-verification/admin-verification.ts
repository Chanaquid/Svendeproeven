import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { VerificationRequestService } from '../../services/verificationRequestService';
import {
  VerificationRequestDto,
  VerificationRequestListDto,
} from '../../dtos/verificationRequestDto';
import { VerificationRequestFilter } from '../../dtos/filterDto';
import { VerificationDocumentType, VerificationStatus } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'pending' | 'approved' | 'rejected';

@Component({
  selector: 'app-admin-verification',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './admin-verification.html',
  styleUrl: './admin-verification.css',
})
export class AdminVerification implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadVerifications(); };

  verifications: VerificationRequestListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pending';

  currentPage = 1;
  totalCount = 0;

  showModal = false;
  isLoadingDetail = false;
  selectedItem: VerificationRequestListDto | null = null;
  detail: VerificationRequestDto | null = null;

  adminNote = '';
  decisionError = '';
  decisionSuccess = '';
  isDeciding = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',      label: 'Alle' },
    { key: 'pending',  label: 'Afventer' },
    { key: 'approved', label: 'Godkendt' },
    { key: 'rejected', label: 'Afvist' },
  ];

  readonly VerificationStatus = VerificationStatus;

  constructor(
    private authService: AuthService,
    private verificationService: VerificationRequestService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 96));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadVerifications();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadVerifications();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadVerifications(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, VerificationStatus>> = {
      pending:  VerificationStatus.Pending,
      approved: VerificationStatus.Approved,
      rejected: VerificationStatus.Rejected,
    };

    const filter: VerificationRequestFilter = {
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'submittedAt',
      sortDescending: true,
    };

    this.verificationService
      .getAll(filter, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.verifications = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente verifikationsanmodninger.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'submittedAt', sortDescending: true };

    this.verificationService
      .getAll(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const tab = this.tabs.find((t) => t.key === 'all');
          if (tab) {
            tab.count = res.data?.totalCount ?? 0;
            this.cdr.detectChanges();
          }
        },
      });

    const statusTabs: { key: TabKey; status: VerificationStatus }[] = [
      { key: 'pending',  status: VerificationStatus.Pending },
      { key: 'approved', status: VerificationStatus.Approved },
      { key: 'rejected', status: VerificationStatus.Rejected },
    ];

    for (const { key, status } of statusTabs) {
      this.verificationService
        .getAll({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            const tab = this.tabs.find((t) => t.key === key);
            if (tab) {
              tab.count = res.data?.totalCount ?? 0;
              this.cdr.detectChanges();
            }
          },
        });
    }
  }

  switchTab(key: TabKey): void {
    this.activeTab = key;
    this.currentPage = 1;
    this.loadVerifications();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadVerifications();
  }

  openModal(item: VerificationRequestListDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.adminNote = '';
    this.decisionError = '';
    this.decisionSuccess = '';

    this.verificationService
      .getById(item.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoadingDetail = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => { this.detail = res.data ?? null; },
        error: () => { this.showModal = false; },
      });
  }

  closeModal(): void {
    this.showModal = false;
    this.detail = null;
  }

  get canDecide(): boolean {
    return this.detail?.status === VerificationStatus.Pending;
  }

  decide(status: VerificationStatus): void {
    if (!this.detail) return;
    this.isDeciding = true;
    this.decisionError = '';

    this.verificationService
      .decide(this.detail.id, { status, adminNote: this.adminNote.trim() || undefined })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isDeciding = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.decisionSuccess =
            status === VerificationStatus.Approved
              ? 'Verifikation godkendt.'
              : 'Verifikation afvist.';
          this.loadVerifications();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.decisionSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.decisionError = err.error?.message ?? 'Kunne ikke behandle afgørelsen.';
        },
      });
  }

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

  getDocTypeLabel(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return 'Pas';
      case VerificationDocumentType.NationalId:     return 'ID-kort';
      case VerificationDocumentType.DrivingLicense: return 'Kørekort';
      default:                                       return type as string;
    }
  }

  getDocTypeIcon(type: VerificationDocumentType | string): string {
    switch (type) {
      case VerificationDocumentType.Passport:       return '🛂';
      case VerificationDocumentType.NationalId:     return '🪪';
      case VerificationDocumentType.DrivingLicense: return '🚗';
      default:                                       return '📄';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
