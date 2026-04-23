import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AppealStatus, AppealType, FineStatus } from '../../dtos/enums';
import { debounceTime, distinctUntilChanged, finalize, Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppealDto, CreateFineAppealDto, CreateScoreAppealDto } from '../../dtos/appealDto';
import { AppealService } from '../../services/appealService';
import { Router } from '@angular/router';
import { PagedRequest } from '../../dtos/paginationDto';
import { AppealFilter } from '../../dtos/filterDto';
import { FineListDto } from '../../dtos/fineDto';
import { FineService } from '../../services/fineService';

type TabId = 'all' | 'pending' | 'approved' | 'rejected' | 'cancelled';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  status?: AppealStatus;
  count?: number;
}

@Component({
  selector: 'app-appeal',
  imports: [CommonModule, FormsModule],
  templateUrl: './appeal.html',
  styleUrl: './appeal.css',
})


export class Appeal implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',       label: 'All',       icon: '▤' },
    { id: 'pending',   label: 'Pending',   icon: '⏳', status: AppealStatus.Pending },
    { id: 'approved',  label: 'Approved',  icon: '✓',  status: AppealStatus.Approved },
    { id: 'rejected',  label: 'Rejected',  icon: '✕',  status: AppealStatus.Rejected },
    { id: 'cancelled', label: 'Cancelled', icon: '—',  status: AppealStatus.Cancelled },
  ];
  activeTab: TabId = 'all';

  // List state
  appeals: AppealDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  searchQuery = '';
  sortFilter = 'newest';

  sortOptions = [
    { value: 'newest', label: 'Newest first' },
    { value: 'oldest', label: 'Oldest first' },
  ];

  // Detail state
  selectedId: number | null = null;
  selectedAppeal: AppealDto | null = null;
  detailLoading = false;
  detailError: string | null = null;
  isRefreshingDetail = false;

  // Create appeal
  showCreateForm = false;
  createType: AppealType = AppealType.Score;
  createMessage = '';
  createFineId: number | null = null;
  isCreating = false;
  createError = '';
  createSuccess = '';

  // Cancel
  showCancelConfirm = false;
  isCancelling = false;


  //Fines
  appealableFines: FineListDto[] = [];
  loadingFines = false;


  readonly AppealType = AppealType;
  readonly AppealStatus = AppealStatus;

  constructor(
    private appealService: AppealService,
    private fineService: FineService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.loadAppeals();
    this.loadTabCounts();
    this.loadAppealableFines();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadAppeals();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAppeals(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find(t => t.id === this.activeTab)?.status ?? null;

    const filter: AppealFilter = {
      search: this.searchQuery?.trim() || null,
      status: status,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    this.appealService.getMyAppeals(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.appeals = res.data.items;
            this.totalPages = res.data.totalPages;
            const tab = this.tabs.find(t => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load appeals.';
          }
        },
        error: () => { this.listError = 'An error occurred. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.appealService.getMyAppeals(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => {
        const tab = this.tabs.find(t => t.id === 'all');
        if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
      }});

    const statusTabs: { id: TabId; status: AppealStatus }[] = [
      { id: 'pending',   status: AppealStatus.Pending },
      { id: 'approved',  status: AppealStatus.Approved },
      { id: 'rejected',  status: AppealStatus.Rejected },
      { id: 'cancelled', status: AppealStatus.Cancelled },
    ];

    for (const { id, status } of statusTabs) {
      this.appealService.getMyAppeals({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({ next: (res) => {
          const tab = this.tabs.find(t => t.id === id);
          if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
        }});
    }
  }

  private loadAppealableFines(): void {
  this.loadingFines = true;
  const request = { page: 1, pageSize: 100, sortBy: 'createdAt', sortDescending: true };

  this.fineService.getMyFines({ status: FineStatus.Unpaid }, request)
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        const unpaid = res.data?.items ?? [];
        // Also load rejected fines
        this.fineService.getMyFines({ status: FineStatus.Rejected }, request)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (res2) => {
              const rejected = res2.data?.items ?? [];
              // Combine and filter out fines that already have an appeal
              this.appealableFines = [...unpaid, ...rejected]
                .filter(f => !f.hasPendingAppeal);
              this.loadingFines = false;
              this.cdr.markForCheck();
            },
            error: () => {
              this.appealableFines = unpaid.filter(f => !f.hasPendingAppeal);
              this.loadingFines = false;
              this.cdr.markForCheck();
            }
          });
      },
      error: () => {
        this.loadingFines = false;
        this.cdr.markForCheck();
      }
    });
  }

  openAppeal(id: number): void {
    if (this.selectedId === id) return;

    const isFirstOpen = this.selectedAppeal === null;
    this.selectedId = id;
    this.detailError = null;

    if (isFirstOpen) {
      this.detailLoading = true;
    } else {
      this.isRefreshingDetail = true;
    }

    this.appealService.getById(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.detailLoading = false;
        this.isRefreshingDetail = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedAppeal = res.data;
          }
        },
        error: (err) => {
          this.selectedId = null;
          this.detailError = err.error?.message ?? 'Failed to load appeal.';
          this.cdr.markForCheck();
        },
      });
  }

  submitAppeal(): void {
    if (!this.createMessage.trim() || this.createMessage.trim().length < 20) {
      this.createError = 'Message must be at least 20 characters.';
      return;
    }

    if (this.createType === AppealType.Fine && !this.createFineId) {
      this.createError = 'Please enter a Fine ID.';
      return;
    }

    this.isCreating = true;
    this.createError = '';

    const obs$ = this.createType === AppealType.Score
      ? this.appealService.createScoreAppeal({ message: this.createMessage.trim() })
      : this.appealService.createFineAppeal({ fineId: this.createFineId!, message: this.createMessage.trim() });

    obs$.pipe(takeUntil(this.destroy$), finalize(() => {
      this.isCreating = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.createSuccess = 'Appeal submitted successfully!';
          this.showCreateForm = false;
          this.createMessage = '';
          this.createFineId = null;
          this.loadAppeals();
          this.loadTabCounts();
          setTimeout(() => { this.createSuccess = ''; this.cdr.markForCheck(); }, 4000);
        }
      },
      error: (err) => {
        this.createError = err.error?.message ?? 'Failed to submit appeal.';
      },
    });
  }

  cancelAppeal(): void {
    if (!this.selectedAppeal) return;
    this.isCancelling = true;

    this.appealService.cancelAppeal(this.selectedAppeal.id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isCancelling = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.showCancelConfirm = false;
          this.selectedId = null;
          this.selectedAppeal = null;
          this.loadAppeals();
          this.loadTabCounts();
        },
        error: (err) => {
          this.createError = err.error?.message ?? 'Failed to cancel appeal.';
          this.cdr.markForCheck();
        },
      });
  }

  canCancel(): boolean {
    return this.selectedAppeal?.status === AppealStatus.Pending;
  }

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadAppeals();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  onFilterChange(): void { this.currentPage = 1; this.loadAppeals(); }
  goToPage(p: number): void { this.currentPage = p; this.loadAppeals(); }
  trackById(_: number, a: AppealDto): number { return a.id; }

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }

  getStatusClass(status: AppealStatus | string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case AppealStatus.Approved:  return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case AppealStatus.Rejected:  return 'bg-red-400/10 text-red-400 border-red-400/20';
      case AppealStatus.Cancelled: return 'bg-zinc-700 text-zinc-400 border-zinc-700';
      default:                     return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getTypeClass(type: AppealType): string {
    switch (type) {
      case AppealType.Score: return 'bg-blue-400/10 text-blue-400';
      case AppealType.Fine:  return 'bg-purple-400/10 text-purple-400';
      default:               return 'bg-zinc-800 text-zinc-400';
    }
  }

  get createMessageTooShort(): boolean {
    return this.createMessage.trim().length > 0 && this.createMessage.trim().length < 20;
  }
}
