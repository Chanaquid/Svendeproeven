import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { AppealService } from '../../services/appealService';
import { AdminAppealDto, AppealDto } from '../../dtos/appealDto';
import { AppealFilter } from '../../dtos/filterDto';
import { AppealStatus, AppealType, FineAppealResolution } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'pending' | 'approved' | 'rejected' | 'cancelled';

@Component({
  selector: 'app-admin-appeal',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-appeal.html',
  styleUrl: './admin-appeal.css',
})
export class AdminAppeal implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadAppeals(); };

  appeals: AppealDto[] = [];
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
  selectedItem: AppealDto | null = null;
  detail: AdminAppealDto | null = null;

  // Decision state
  showDecideForm = false;
  decideIsApproved: boolean | null = null;
  decideAdminNote = '';

  // Fine appeal decision extras
  decideFineResolution: FineAppealResolution | '' = '';
  decideCustomFineAmount: number | null = null;

  // Score appeal decision extras
  decideNewScore: number | null = null;

  decideError = '';
  decideSuccess = '';
  isDeciding = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',       label: 'All' },
    { key: 'pending',   label: 'Pending' },
    { key: 'approved',  label: 'Approved' },
    { key: 'rejected',  label: 'Rejected' },
    { key: 'cancelled', label: 'Cancelled' },
  ];

  readonly AppealStatus       = AppealStatus;
  readonly AppealType         = AppealType;
  readonly FineAppealResolution = FineAppealResolution;

  constructor(
    private authService: AuthService,
    private appealService: AppealService,
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

    this.loadAppeals();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => { this.currentPage = 1; this.loadAppeals(); });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  loadAppeals(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, AppealStatus>> = {
      pending:   AppealStatus.Pending,
      approved:  AppealStatus.Approved,
      rejected:  AppealStatus.Rejected,
      cancelled: AppealStatus.Cancelled,
    };

    const filter: AppealFilter = {
      status: this.activeTab !== 'all' ? (statusMap[this.activeTab] ?? null) : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.appealService.adminGetAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.appeals = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find(t => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => { this.listError = 'Failed to load appeals. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.appealService.adminGetAll(null, request).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const tab = this.tabs.find(t => t.key === 'all');
        if (tab) { tab.count = res.data?.totalCount ?? 0; this.cdr.detectChanges(); }
      }
    });

    const statusTabs: { key: TabKey; status: AppealStatus }[] = [
      { key: 'pending',   status: AppealStatus.Pending },
      { key: 'approved',  status: AppealStatus.Approved },
      { key: 'rejected',  status: AppealStatus.Rejected },
      { key: 'cancelled', status: AppealStatus.Cancelled },
    ];

    for (const { key, status } of statusTabs) {
      this.appealService.adminGetAll({ status }, request).pipe(takeUntil(this.destroy$)).subscribe({
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
    this.loadAppeals();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadAppeals();
  }

  // ─── Modal ───────────────────────────────────────────────────────────────

  openModal(item: AppealDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.resetDecideForm();

    // Use the dedicated admin endpoint which returns AdminAppealDto with full user stats.
    this.appealService.adminGetById(item.id)
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
  }

  // ─── Decide ───────────────────────────────────────────────────────────────

  get canDecide(): boolean {
    return this.detail?.status === AppealStatus.Pending;
  }

  get isFineAppeal(): boolean {
    return this.detail?.appealType === AppealType.Fine;
  }

  get isScoreAppeal(): boolean {
    return this.detail?.appealType === AppealType.Score;
  }

  get showCustomAmount(): boolean {
    return this.decideIsApproved === true
        && this.isFineAppeal
        && this.decideFineResolution === FineAppealResolution.Custom;
  }

  get showNewScore(): boolean {
    return this.decideIsApproved === true && this.isScoreAppeal;
  }

  get decideFormValid(): boolean {
    if (this.decideIsApproved === null) return false;

    if (this.decideIsApproved) {
      if (this.isFineAppeal) {
        if (!this.decideFineResolution) return false;
        if (this.decideFineResolution === FineAppealResolution.Custom) {
          return this.decideCustomFineAmount !== null && this.decideCustomFineAmount > 0;
        }
      }
      // score appeal — newScore is optional (backend keeps current if not supplied)
    }

    return true;
  }

  openDecideForm(approve: boolean): void {
    this.decideIsApproved = approve;
    this.decideFineResolution = '';
    this.decideCustomFineAmount = null;
    this.decideNewScore = null;
    this.decideError = '';
    this.showDecideForm = true;
  }

  public resetDecideForm(): void {
    this.showDecideForm = false;
    this.decideIsApproved = null;
    this.decideAdminNote = '';
    this.decideFineResolution = '';
    this.decideCustomFineAmount = null;
    this.decideNewScore = null;
    this.decideError = '';
    this.decideSuccess = '';
    this.isDeciding = false;
  }

  submitDecision(): void {
    if (!this.detail || !this.decideFormValid) return;
    this.isDeciding = true;
    this.decideError = '';

    const note = this.decideAdminNote.trim() || undefined;

    const call$ = this.isFineAppeal
      ? this.appealService.adminDecideFine(this.detail.id, {
          isApproved: this.decideIsApproved!,
          adminNote: note,
          resolution: this.decideIsApproved && this.decideFineResolution
            ? this.decideFineResolution as FineAppealResolution
            : undefined,
          customFineAmount: this.decideIsApproved && this.decideFineResolution === FineAppealResolution.Custom
            ? (this.decideCustomFineAmount ?? undefined)
            : undefined,
        })
      : this.appealService.adminDecideScore(this.detail.id, {
          isApproved: this.decideIsApproved!,
          adminNote: note,
          newScore: this.decideIsApproved && this.decideNewScore !== null
            ? this.decideNewScore
            : undefined,
        });

    call$.pipe(takeUntil(this.destroy$), finalize(() => {
      this.isDeciding = false;
      this.cdr.detectChanges();
    })).subscribe({
      next: (res) => {
        this.detail = res.data as AdminAppealDto;
        this.decideSuccess = this.decideIsApproved ? 'Appeal approved.' : 'Appeal rejected.';
        this.showDecideForm = false;
        this.loadAppeals();
        this.loadTabCounts();
        setTimeout(() => {
          this.showModal = false;
          this.decideSuccess = '';
          this.cdr.detectChanges();
        }, 1500);
      },
      error: (err) => { this.decideError = err.error?.message ?? 'Failed to process decision.'; },
    });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  getStatusClass(status: string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case AppealStatus.Approved:  return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case AppealStatus.Rejected:  return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      case AppealStatus.Cancelled: return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      case AppealStatus.Deleted:   return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      default:                     return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getTypeClass(type: string): string {
    switch (type) {
      case AppealType.Fine:  return 'bg-red-400/10 text-red-400';
      case AppealType.Score: return 'bg-blue-400/10 text-blue-400';
      default:               return 'bg-zinc-800 text-zinc-400';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case AppealType.Fine:  return '💸 Fine Appeal';
      case AppealType.Score: return '⭐ Score Appeal';
      default:               return type;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}