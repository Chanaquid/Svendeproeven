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
  imports: [CommonModule, FormsModule, Navbar],
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

  currentPage = 1;
  totalCount = 0;

  showModal = false;
  isLoadingDetail = false;
  selectedItem: AppealDto | null = null;
  detail: AdminAppealDto | null = null;

  showDecideForm = false;
  decideIsApproved: boolean | null = null;
  decideAdminNote = '';
  decideFineResolution: FineAppealResolution | '' = '';
  decideCustomFineAmount: number | null = null;
  decideNewScore: number | null = null;

  decideError = '';
  decideSuccess = '';
  isDeciding = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',       label: 'Alle' },
    { key: 'pending',   label: 'Afventer' },
    { key: 'approved',  label: 'Godkendt' },
    { key: 'rejected',  label: 'Afvist' },
    { key: 'cancelled', label: 'Annulleret' },
  ];

  readonly AppealStatus = AppealStatus;
  readonly AppealType = AppealType;
  readonly FineAppealResolution = FineAppealResolution;

  constructor(
    private authService: AuthService,
    private appealService: AppealService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 88));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadAppeals();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadAppeals();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

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
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.appealService
      .adminGetAll(filter, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.appeals = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente klager. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.appealService
      .adminGetAll(null, request)
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

    const statusTabs: { key: TabKey; status: AppealStatus }[] = [
      { key: 'pending',   status: AppealStatus.Pending },
      { key: 'approved',  status: AppealStatus.Approved },
      { key: 'rejected',  status: AppealStatus.Rejected },
      { key: 'cancelled', status: AppealStatus.Cancelled },
    ];

    for (const { key, status } of statusTabs) {
      this.appealService
        .adminGetAll({ status }, request)
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
    this.loadAppeals();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadAppeals();
  }

  openModal(item: AppealDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.resetDecideForm();

    this.appealService
      .adminGetById(item.id)
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

  get canDecide(): boolean { return this.detail?.status === AppealStatus.Pending; }
  get isFineAppeal(): boolean { return this.detail?.appealType === AppealType.Fine; }
  get isScoreAppeal(): boolean { return this.detail?.appealType === AppealType.Score; }

  get showCustomAmount(): boolean {
    return (
      this.decideIsApproved === true &&
      this.isFineAppeal &&
      this.decideFineResolution === FineAppealResolution.Custom
    );
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
          resolution:
            this.decideIsApproved && this.decideFineResolution
              ? (this.decideFineResolution as FineAppealResolution)
              : undefined,
          customFineAmount:
            this.decideIsApproved && this.decideFineResolution === FineAppealResolution.Custom
              ? this.decideCustomFineAmount ?? undefined
              : undefined,
        })
      : this.appealService.adminDecideScore(this.detail.id, {
          isApproved: this.decideIsApproved!,
          adminNote: note,
          newScore:
            this.decideIsApproved && this.decideNewScore !== null
              ? this.decideNewScore
              : undefined,
        });

    call$
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isDeciding = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data as AdminAppealDto;
          this.decideSuccess = this.decideIsApproved ? 'Klage godkendt.' : 'Klage afvist.';
          this.showDecideForm = false;
          this.loadAppeals();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.decideSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.decideError = err.error?.message ?? 'Kunne ikke behandle afgørelsen.';
        },
      });
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'badge badge-warning';
      case AppealStatus.Approved:  return 'badge badge-success';
      case AppealStatus.Rejected:  return 'badge badge-danger';
      case AppealStatus.Cancelled:
      case AppealStatus.Deleted:   return 'badge';
      default:                     return 'badge';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case AppealStatus.Pending:   return 'Afventer';
      case AppealStatus.Approved:  return 'Godkendt';
      case AppealStatus.Rejected:  return 'Afvist';
      case AppealStatus.Cancelled: return 'Annulleret';
      case AppealStatus.Deleted:   return 'Slettet';
      default:                     return status;
    }
  }

  getTypeBadge(type: string): string {
    switch (type) {
      case AppealType.Fine:  return 'badge badge-danger';
      case AppealType.Score: return 'badge badge-info';
      default:               return 'badge';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case AppealType.Fine:  return '💸 Bøde-klage';
      case AppealType.Score: return '⭐ Score-klage';
      default:               return type;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
