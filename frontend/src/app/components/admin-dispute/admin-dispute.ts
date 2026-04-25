import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { DisputeService } from '../../services/disputeService';
import {
  AdminResolveDisputeDto,
  DisputeDto,
  DisputeListDto,
  DisputePenaltyDto,
} from '../../dtos/disputeDto';
import { DisputeFilter } from '../../dtos/filterDto';
import { DisputeFiledAs, DisputeStatus, DisputeVerdict, ItemCondition } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'awaiting' | 'pending' | 'overdue' | 'resolved' | 'cancelled';

@Component({
  selector: 'app-admin-dispute',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-dispute.html',
  styleUrl: './admin-dispute.css',
})
export class AdminDispute implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadDisputes(); };

  disputes: DisputeListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pending';

  // Pagination
  currentPage = 1;
  totalCount = 0;

  // Modal
  showModal = false;
  isLoadingDetail = false;
  selectedItem: DisputeListDto | null = null;
  detail: DisputeDto | null = null;
  selectedPhoto: string | null = null;
  selectedPhotoCaption: string | null = null;

  // Resolve
  resolveVerdict: DisputeVerdict | '' = '';
  resolveAdminNote = '';
  // Owner penalty
  ownerFine: number | null = null;
  ownerScore: number | null = null;
  // Borrower penalty
  borrowerFine: number | null = null;
  borrowerScore: number | null = null;

  resolveError = '';
  resolveSuccess = '';
  isResolving = false;
  showResolveForm = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',      label: 'All' },
    { key: 'awaiting', label: 'Awaiting Response' },
    { key: 'pending',  label: 'Under Review' },
    { key: 'overdue',  label: 'Overdue' },
    { key: 'resolved', label: 'Resolved' },
    { key: 'cancelled',label: 'Cancelled' },
  ];

  readonly DisputeStatus  = DisputeStatus;
  readonly DisputeVerdict = DisputeVerdict;
  readonly DisputeFiledAs = DisputeFiledAs;

  verdictOptions = [
    { value: DisputeVerdict.NoPenalty,         label: 'No Penalty',          desc: 'Close without penalising either party' },
    { value: DisputeVerdict.OwnerPenalized,    label: 'Owner Penalised',     desc: 'The owner bears responsibility' },
    { value: DisputeVerdict.BorrowerPenalized, label: 'Borrower Penalised',  desc: 'The borrower bears responsibility' },
    { value: DisputeVerdict.BothPenalized,     label: 'Both Penalised',      desc: 'Both parties share responsibility' },
  ];

  constructor(
    private authService: AuthService,
    private disputeService: DisputeService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Dynamic page size ────────────────────────────────────────────────────
  // navbar 64 + header ~200 + tabs 48 + search 52 + pagination 56 + padding 80
  // Each card ~96px

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 96));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  // ─── Lifecycle ───────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadDisputes();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => { this.currentPage = 1; this.loadDisputes(); });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  loadDisputes(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, DisputeStatus>> = {
      awaiting: DisputeStatus.AwaitingResponse,
      pending:  DisputeStatus.PendingAdminReview,
      overdue:  DisputeStatus.PastDeadline,
      resolved: DisputeStatus.Resolved,
      cancelled:DisputeStatus.Cancelled,
    };

    const filter: DisputeFilter = {
      status: this.activeTab !== 'all' ? (statusMap[this.activeTab] ?? null) : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.disputeService.adminGetAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.disputes = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find(t => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => { this.listError = 'Failed to load disputes. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.disputeService.adminGetAll(null, request).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const tab = this.tabs.find(t => t.key === 'all');
        if (tab) { tab.count = res.data?.totalCount ?? 0; this.cdr.detectChanges(); }
      }
    });

    const statusTabs: { key: TabKey; status: DisputeStatus }[] = [
      { key: 'awaiting', status: DisputeStatus.AwaitingResponse },
      { key: 'pending',  status: DisputeStatus.PendingAdminReview },
      { key: 'overdue',  status: DisputeStatus.PastDeadline },
      { key: 'resolved', status: DisputeStatus.Resolved },
      { key: 'cancelled',status: DisputeStatus.Cancelled },
    ];

    for (const { key, status } of statusTabs) {
      this.disputeService.adminGetAll({ status }, request).pipe(takeUntil(this.destroy$)).subscribe({
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
    this.loadDisputes();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadDisputes();
  }

  // ─── Modal ───────────────────────────────────────────────────────────────

  openModal(item: DisputeListDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.resetResolveForm();
    this.selectedPhoto = null;

    this.disputeService.adminGetById(item.id)
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

  // ─── Resolve ─────────────────────────────────────────────────────────────

  get canResolve(): boolean {
    return this.detail?.status === DisputeStatus.PendingAdminReview
        || this.detail?.status === DisputeStatus.PastDeadline
        || this.detail?.status === DisputeStatus.AwaitingResponse;
  }

  get showOwnerPenalty(): boolean {
    return this.resolveVerdict === DisputeVerdict.OwnerPenalized
        || this.resolveVerdict === DisputeVerdict.BothPenalized;
  }

  get showBorrowerPenalty(): boolean {
    return this.resolveVerdict === DisputeVerdict.BorrowerPenalized
        || this.resolveVerdict === DisputeVerdict.BothPenalized;
  }

  private resetResolveForm(): void {
    this.showResolveForm = false;
    this.resolveVerdict = '';
    this.resolveAdminNote = '';
    this.ownerFine = null;
    this.ownerScore = null;
    this.borrowerFine = null;
    this.borrowerScore = null;
    this.resolveError = '';
    this.resolveSuccess = '';
    this.isResolving = false;
  }

  resolve(): void {
    if (!this.detail || !this.resolveVerdict) return;
    this.isResolving = true;
    this.resolveError = '';

    const ownerPenalty: DisputePenaltyDto | null = this.showOwnerPenalty
      ? { fineAmount: this.ownerFine ?? undefined, scoreAdjustment: this.ownerScore ?? undefined }
      : null;

    const borrowerPenalty: DisputePenaltyDto | null = this.showBorrowerPenalty
      ? { fineAmount: this.borrowerFine ?? undefined, scoreAdjustment: this.borrowerScore ?? undefined }
      : null;

    const dto: AdminResolveDisputeDto = {
      verdict: this.resolveVerdict as DisputeVerdict,
      adminNote: this.resolveAdminNote.trim() || undefined,
      ownerPenalty,
      borrowerPenalty,
    };

    this.disputeService.adminResolve(this.detail.id, dto)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isResolving = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.resolveSuccess = 'Dispute resolved successfully.';
          this.showResolveForm = false;
          this.loadDisputes();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.resolveSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => { this.resolveError = err.error?.message ?? 'Failed to resolve dispute.'; },
      });
  }

  // ─── UI helpers ──────────────────────────────────────────────────────────

  openPhoto(url: string, caption?: string | null): void {
    this.selectedPhoto = url;
    this.selectedPhotoCaption = caption ?? null;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case DisputeStatus.AwaitingResponse:   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case DisputeStatus.PendingAdminReview: return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case DisputeStatus.Resolved:           return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case DisputeStatus.PastDeadline:       return 'bg-red-400/10 text-red-400 border-red-400/20';
      case DisputeStatus.Cancelled:          return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
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

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }
}