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

import { AppealStatus, AppealType, FineStatus } from '../../dtos/enums';
import { AppealDto } from '../../dtos/appealDto';
import { FineListDto } from '../../dtos/fineDto';
import { AppealService } from '../../services/appealService';
import { FineService } from '../../services/fineService';
import { PagedRequest } from '../../dtos/paginationDto';
import { AppealFilter } from '../../dtos/filterDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

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
export class Appeal implements OnInit, OnChanges, OnDestroy {
  @Input() openAppealId: number | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => {
    this.currentPage = 1;
    this.loadAppeals();
  };

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',       label: 'Alle',        icon: '▤' },
    { id: 'pending',   label: 'Afventer',    icon: '⏳', status: AppealStatus.Pending },
    { id: 'approved',  label: 'Godkendt',    icon: '✓',  status: AppealStatus.Approved },
    { id: 'rejected',  label: 'Afvist',      icon: '✕',  status: AppealStatus.Rejected },
    { id: 'cancelled', label: 'Annulleret',  icon: '—',  status: AppealStatus.Cancelled },
  ];
  activeTab: TabId = 'all';

  // List state
  appeals: AppealDto[] = [];
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

  // Fines selector — paginated
  appealableFines: FineListDto[] = [];
  loadingFines = false;
  finesPage = 1;
  finesTotalCount = 0;
  readonly FINES_PAGE_SIZE = 5;

  readonly AppealType = AppealType;
  readonly AppealStatus = AppealStatus;

  constructor(
    private appealService: AppealService,
    private fineService: FineService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  get pageSize(): number {
    const availableHeight = window.innerHeight - 64 - 52 - 48 - 80;
    return Math.max(5, Math.floor(availableHeight / 90));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  get finesTotalPages(): number { return getTotalPages(this.finesTotalCount, this.FINES_PAGE_SIZE); }
  get finesPageNumbers(): number[] { return getPageNumbers(this.finesPage, this.finesTotalPages); }

  ngOnInit(): void {
    this.loadAppeals();
    this.loadTabCounts();
    this.loadAppealableFines();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadAppeals();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['openAppealId'] && this.openAppealId) {
      this.openAppeal(this.openAppealId);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAppeals(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find((t) => t.id === this.activeTab)?.status ?? null;

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

    this.appealService
      .getMyAppeals(filter, request)
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
            this.appeals = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find((t) => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Kunne ikke hente klager.';
          }
        },
        error: () => {
          this.listError = 'Der opstod en fejl. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.appealService
      .getMyAppeals(null, request)
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

    const statusTabs: { id: TabId; status: AppealStatus }[] = [
      { id: 'pending',   status: AppealStatus.Pending },
      { id: 'approved',  status: AppealStatus.Approved },
      { id: 'rejected',  status: AppealStatus.Rejected },
      { id: 'cancelled', status: AppealStatus.Cancelled },
    ];

    for (const { id, status } of statusTabs) {
      this.appealService
        .getMyAppeals({ status }, request)
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

  loadAppealableFines(page = 1): void {
    this.loadingFines = true;
    this.finesPage = page;

    const request: PagedRequest = {
      page: 1,
      pageSize: 200,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.fineService
      .getMyFines({ status: FineStatus.Unpaid }, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const unpaid = res.data?.items ?? [];
          this.fineService
            .getMyFines({ status: FineStatus.Rejected }, request)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (res2) => {
                const rejected = res2.data?.items ?? [];
                const all = [...unpaid, ...rejected].filter((f) => !f.hasPendingAppeal);
                this.finesTotalCount = all.length;
                const start = (this.finesPage - 1) * this.FINES_PAGE_SIZE;
                this.appealableFines = all.slice(start, start + this.FINES_PAGE_SIZE);
                this.loadingFines = false;
                this.cdr.markForCheck();
              },
              error: () => {
                const all = unpaid.filter((f) => !f.hasPendingAppeal);
                this.finesTotalCount = all.length;
                const start = (this.finesPage - 1) * this.FINES_PAGE_SIZE;
                this.appealableFines = all.slice(start, start + this.FINES_PAGE_SIZE);
                this.loadingFines = false;
                this.cdr.markForCheck();
              },
            });
        },
        error: () => {
          this.loadingFines = false;
          this.cdr.markForCheck();
        },
      });
  }

  goToFinesPage(p: number): void {
    if (p < 1 || p > this.finesTotalPages) return;
    this.loadAppealableFines(p);
  }

  openAppeal(id: number): void {
    if (this.selectedId === id) return;

    const isFirstOpen = this.selectedAppeal === null;
    this.selectedId = id;
    this.detailError = null;

    if (isFirstOpen) this.detailLoading = true;
    else this.isRefreshingDetail = true;

    this.appealService
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
          if (res.success && res.data) this.selectedAppeal = res.data;
        },
        error: (err) => {
          this.selectedId = null;
          this.detailError = err.error?.message ?? 'Kunne ikke indlæse klage.';
          this.cdr.markForCheck();
        },
      });
  }

  submitAppeal(): void {
    if (!this.createMessage.trim() || this.createMessage.trim().length < 20) {
      this.createError = 'Beskeden skal være mindst 20 tegn.';
      return;
    }

    if (this.createType === AppealType.Fine && !this.createFineId) {
      this.createError = 'Vælg venligst en bøde.';
      return;
    }

    this.isCreating = true;
    this.createError = '';

    const obs$ =
      this.createType === AppealType.Score
        ? this.appealService.createScoreAppeal({ message: this.createMessage.trim() })
        : this.appealService.createFineAppeal({
            fineId: this.createFineId!,
            message: this.createMessage.trim(),
          });

    obs$
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
            this.createSuccess = 'Klage indsendt!';
            this.showCreateForm = false;
            this.createMessage = '';
            this.createFineId = null;
            this.loadAppeals();
            this.loadTabCounts();
            this.loadAppealableFines();
            setTimeout(() => {
              this.createSuccess = '';
              this.cdr.markForCheck();
            }, 4000);
          }
        },
        error: (err) => {
          this.createError = err.error?.message ?? 'Kunne ikke indsende klage.';
        },
      });
  }

  cancelAppeal(): void {
    if (!this.selectedAppeal) return;
    this.isCancelling = true;

    this.appealService
      .cancelAppeal(this.selectedAppeal.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isCancelling = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: () => {
          this.showCancelConfirm = false;
          this.selectedId = null;
          this.selectedAppeal = null;
          this.loadAppeals();
          this.loadTabCounts();
        },
        error: (err) => {
          this.createError = err.error?.message ?? 'Kunne ikke annullere klage.';
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

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadAppeals();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadAppeals();
  }

  trackById(_: number, a: AppealDto): number { return a.id; }

  getStatusBadge(status: AppealStatus | string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'badge badge-warning';
      case AppealStatus.Approved:  return 'badge badge-success';
      case AppealStatus.Rejected:  return 'badge badge-danger';
      case AppealStatus.Cancelled: return 'badge';
      default:                     return 'badge';
    }
  }

  getStatusLabel(status: AppealStatus | string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'Afventer';
      case AppealStatus.Approved:  return 'Godkendt';
      case AppealStatus.Rejected:  return 'Afvist';
      case AppealStatus.Cancelled: return 'Annulleret';
      default:                     return status as string;
    }
  }

  getTypeBadge(type: AppealType): string {
    switch (type) {
      case AppealType.Score: return 'badge badge-info';
      case AppealType.Fine:  return 'badge badge-warning';
      default:               return 'badge';
    }
  }

  getTypeLabel(type: AppealType): string {
    switch (type) {
      case AppealType.Score: return 'Score';
      case AppealType.Fine:  return 'Bøde';
      default:               return type as unknown as string;
    }
  }

  get createMessageTooShort(): boolean {
    return this.createMessage.trim().length > 0 && this.createMessage.trim().length < 20;
  }
}
