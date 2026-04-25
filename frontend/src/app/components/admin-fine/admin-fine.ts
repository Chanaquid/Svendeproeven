import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { FineService } from '../../services/fineService';
import { FineDto, FineListDto } from '../../dtos/fineDto';
import { FineFilter } from '../../dtos/filterDto';
import { FineStatus, FineType } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'unpaid' | 'pendingVerification' | 'paid' | 'rejected' | 'voided';

@Component({
  selector: 'app-admin-fine',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-fine.html',
  styleUrl: './admin-fine.css',
})
export class AdminFine implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadFines(); };

  fines: FineListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pendingVerification';

  // Pagination
  currentPage = 1;
  totalCount = 0;

  // Modal
  showModal = false;
  isLoadingDetail = false;
  selectedItem: FineListDto | null = null;
  detail: FineDto | null = null;
  selectedPhoto: string | null = null;

  // Payment proof review
  proofRejectionReason = '';
  proofError = '';
  proofSuccess = '';
  isProcessingProof = false;
  showRejectProofForm = false;

  // Void fine
  showVoidConfirm = false;
  isVoiding = false;
  voidError = '';

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',                 label: 'All' },
    { key: 'unpaid',              label: 'Unpaid' },
    { key: 'pendingVerification', label: 'Proof Review' },
    { key: 'paid',                label: 'Paid' },
    { key: 'rejected',            label: 'Rejected' },
    { key: 'voided',              label: 'Voided' },
  ];

  readonly FineStatus = FineStatus;
  readonly FineType   = FineType;

  constructor(
    private authService: AuthService,
    private fineService: FineService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Dynamic page size ────────────────────────────────────────────────────
  // navbar 64 + header ~200 + tabs 48 + search 52 + pagination 56 + padding 80
  // Each card ~88px

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 88));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  // ─── Lifecycle ───────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadFines();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => { this.currentPage = 1; this.loadFines(); });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  loadFines(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, FineStatus>> = {
      unpaid:              FineStatus.Unpaid,
      pendingVerification: FineStatus.PendingVerification,
      paid:                FineStatus.Paid,
      rejected:            FineStatus.Rejected,
      voided:              FineStatus.Voided,
    };

    const filter: FineFilter = {
      status: this.activeTab !== 'all' ? (statusMap[this.activeTab] ?? null) : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.fineService.adminGetAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.fines = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find(t => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => { this.listError = 'Failed to load fines. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.fineService.adminGetAll(null, request).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const tab = this.tabs.find(t => t.key === 'all');
        if (tab) { tab.count = res.data?.totalCount ?? 0; this.cdr.detectChanges(); }
      }
    });

    const statusTabs: { key: TabKey; status: FineStatus }[] = [
      { key: 'unpaid',              status: FineStatus.Unpaid },
      { key: 'pendingVerification', status: FineStatus.PendingVerification },
      { key: 'paid',                status: FineStatus.Paid },
      { key: 'rejected',            status: FineStatus.Rejected },
      { key: 'voided',              status: FineStatus.Voided },
    ];

    for (const { key, status } of statusTabs) {
      this.fineService.adminGetAll({ status }, request).pipe(takeUntil(this.destroy$)).subscribe({
        next: (res) => {
          const tab = this.tabs.find(t => t.key === key);
          if (tab) { tab.count = res.data?.totalCount ?? 0; this.cdr.detectChanges(); }
        }
      });
    }
  }

  // ─── Filters / Pagination ─────────────────────────────────────────────────

  switchTab(key: TabKey): void {
    this.activeTab = key;
    this.currentPage = 1;
    this.loadFines();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadFines();
  }

  // ─── Modal ───────────────────────────────────────────────────────────────

  openModal(item: FineListDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.proofRejectionReason = '';
    this.proofError = '';
    this.proofSuccess = '';
    this.showRejectProofForm = false;
    this.showVoidConfirm = false;
    this.voidError = '';
    this.selectedPhoto = null;

    this.fineService.adminGetById(item.id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoadingDetail = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => { this.detail = res.data ?? null; },
        error: () => { this.showModal = false; },
      });
  }

  closeModal(): void {
    this.showModal = false;
    this.detail = null;
    this.selectedPhoto = null;
  }

  // ─── Payment proof review ─────────────────────────────────────────────────

  get hasPendingProof(): boolean {
    return this.detail?.status === FineStatus.PendingVerification
        && !!this.detail?.paymentProofImageUrl;
  }

  approveProof(): void {
    if (!this.detail) return;
    this.isProcessingProof = true;
    this.proofError = '';

    this.fineService.adminVerifyPayment(this.detail.id, { isApproved: true })
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isProcessingProof = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.proofSuccess = 'Payment approved — fine marked as paid.';
          this.loadFines();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.proofSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => { this.proofError = err.error?.message ?? 'Failed to approve payment.'; },
      });
  }

  rejectProof(): void {
    if (!this.detail || !this.proofRejectionReason.trim()) return;
    this.isProcessingProof = true;
    this.proofError = '';

    this.fineService.adminVerifyPayment(this.detail.id, {
      isApproved: false,
      rejectionReason: this.proofRejectionReason.trim(),
    }).pipe(takeUntil(this.destroy$), finalize(() => {
      this.isProcessingProof = false;
      this.cdr.detectChanges();
    })).subscribe({
      next: (res) => {
        this.detail = res.data!;
        this.proofSuccess = 'Payment proof rejected.';
        this.showRejectProofForm = false;
        this.proofRejectionReason = '';
        this.loadFines();
        this.loadTabCounts();
        setTimeout(() => {
          this.showModal = false;
          this.proofSuccess = '';
          this.cdr.detectChanges();
        }, 1500);
      },
      error: (err) => { this.proofError = err.error?.message ?? 'Failed to reject payment.'; },
    });
  }

  // ─── Void fine ────────────────────────────────────────────────────────────

  get canVoid(): boolean {
    const s = this.detail?.status;
    return s === FineStatus.Unpaid || s === FineStatus.Rejected || s === FineStatus.PendingVerification;
  }

  voidFine(): void {
    if (!this.detail) return;
    this.isVoiding = true;
    this.voidError = '';

    this.fineService.adminVoidFine(this.detail.id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isVoiding = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: () => {
          this.showVoidConfirm = false;
          this.showModal = false;
          this.loadFines();
          this.loadTabCounts();
        },
        error: (err) => { this.voidError = err.error?.message ?? 'Failed to void fine.'; },
      });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  getFineStatusClass(status: string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'bg-red-500/10 text-red-400 border-red-500/20';
      case FineStatus.PendingVerification: return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case FineStatus.Paid:                return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case FineStatus.Rejected:            return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      case FineStatus.Voided:              return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      default:                             return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getFineTypeClass(type: string): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'bg-purple-400/10 text-purple-400';
      case FineType.Custom:            return 'bg-blue-400/10 text-blue-400';
      default:                         return 'bg-zinc-800 text-zinc-400';
    }
  }

  getFineTypeLabel(type: string): string {
    switch (type) {
      case FineType.ResultedByDispute: return '⚖️ Dispute';
      case FineType.Custom:            return '✏️ Custom';
      default:                         return type;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}