import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { ReportService } from '../../services/reportService';
import { ItemService } from '../../services/itemService';
import { ReportDto, ReportListDto } from '../../dtos/reportDto';
import { ReportFilter } from '../../dtos/filterDto';
import { ReportReason, ReportStatus, ReportType } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'pending' | 'underReview' | 'resolved' | 'dismissed';
type SortKey = 'newest' | 'oldest' | 'user' | 'item' | 'review' | 'message';

@Component({
  selector: 'app-admin-report',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './admin-report.html',
  styleUrl: './admin-report.css',
})
export class AdminReport implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadReports(); };

  reports: ReportDto[] = [];
  isLoading = true;
  isLoadingTarget = false;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pending';
  sortKey: SortKey = 'newest';
  targetError = '';

  currentPage = 1;
  totalCount = 0;

  showModal = false;
  isLoadingDetail = false;
  selectedItem: ReportDto | null = null;
  detail: ReportDto | null = null;

  resolveStatus: ReportStatus | '' = '';
  resolveNote = '';
  resolveError = '';
  resolveSuccess = '';
  isResolving = false;
  showResolveForm = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',         label: 'Alle' },
    { key: 'pending',     label: 'Afventer' },
    { key: 'underReview', label: 'Under gennemgang' },
    { key: 'resolved',    label: 'Afgjort' },
    { key: 'dismissed',   label: 'Afvist' },
  ];

  sortOptions: { key: SortKey; label: string }[] = [
    { key: 'newest',  label: 'Nyeste først' },
    { key: 'oldest',  label: 'Ældste først' },
    { key: 'user',    label: 'Type: Bruger' },
    { key: 'item',    label: 'Type: Annonce' },
    { key: 'review',  label: 'Type: Anmeldelse' },
    { key: 'message', label: 'Type: Besked' },
  ];

  readonly ReportStatus = ReportStatus;
  readonly ReportType = ReportType;
  readonly ReportReason = ReportReason;

  resolveOptions: { value: ReportStatus; label: string; desc: string }[] = [
    { value: ReportStatus.UnderReview, label: '🔍 Markér under gennemgang', desc: 'Bekræft og start gennemgang' },
    { value: ReportStatus.Resolved,    label: '✓ Afgør',                    desc: 'Handling er foretaget' },
    { value: ReportStatus.Dismissed,   label: '✕ Afvis',                    desc: 'Ikke en gyldig anmeldelse' },
  ];

  constructor(
    private authService: AuthService,
    private reportService: ReportService,
    private itemService: ItemService,
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

    this.loadReports();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadReports();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadReports(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, ReportStatus>> = {
      pending:     ReportStatus.Pending,
      underReview: ReportStatus.UnderReview,
      resolved:    ReportStatus.Resolved,
      dismissed:   ReportStatus.Dismissed,
    };

    const typeFilterMap: Partial<Record<SortKey, ReportType>> = {
      user:    ReportType.User,
      item:    ReportType.Item,
      review:  ReportType.Review,
      message: ReportType.Message,
    };

    const filter: ReportFilter = {
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      type:   typeFilterMap[this.sortKey] ?? null,
      search: this.searchQuery.trim() || null,
    };

    const isTypeSort = this.sortKey in typeFilterMap;
    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: isTypeSort ? true : this.sortKey === 'newest',
    };

    this.reportService
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
          this.reports = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente anmeldelser. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.reportService
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

    const statusTabs: { key: TabKey; status: ReportStatus }[] = [
      { key: 'pending',     status: ReportStatus.Pending },
      { key: 'underReview', status: ReportStatus.UnderReview },
      { key: 'resolved',    status: ReportStatus.Resolved },
      { key: 'dismissed',   status: ReportStatus.Dismissed },
    ];

    for (const { key, status } of statusTabs) {
      this.reportService
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
    this.loadReports();
  }

  onSortChange(): void {
    this.currentPage = 1;
    this.loadReports();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadReports();
  }

  openModal(item: ReportDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.showResolveForm = false;
    this.resolveStatus = '';
    this.resolveNote = '';
    this.resolveError = '';
    this.resolveSuccess = '';
    this.targetError = '';

    this.reportService
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
    this.targetError = '';
  }

  get canResolve(): boolean {
    return (
      this.detail?.status === ReportStatus.Pending ||
      this.detail?.status === ReportStatus.UnderReview
    );
  }

  resolve(): void {
    if (!this.detail || !this.resolveStatus) return;
    this.isResolving = true;
    this.resolveError = '';

    this.reportService
      .adminResolve(this.detail.id, {
        status: this.resolveStatus as ReportStatus,
        adminNote: this.resolveNote.trim() || undefined,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isResolving = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.resolveSuccess = 'Anmeldelse opdateret.';
          this.showResolveForm = false;
          this.loadReports();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.resolveSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.resolveError = err.error?.message ?? 'Kunne ikke opdatere anmeldelse.';
        },
      });
  }

  navigateToTarget(): void {
    if (!this.detail) return;
    this.targetError = '';

    switch (this.detail.type) {
      case ReportType.User:
        this.router.navigate(['/users', this.detail.targetId]);
        break;

      case ReportType.Item:
        this.isLoadingTarget = true;
        this.itemService
          .getById(Number(this.detail.targetId))
          .pipe(
            takeUntil(this.destroy$),
            finalize(() => {
              this.isLoadingTarget = false;
              this.cdr.detectChanges();
            }),
          )
          .subscribe({
            next: (res) => {
              if (res.data?.slug) {
                this.router.navigate(['/items', res.data.slug]);
              } else {
                this.targetError = 'Denne annonce findes ikke længere.';
              }
            },
            error: () => {
              this.targetError = 'Annoncen er slettet eller ikke længere tilgængelig.';
              this.cdr.detectChanges();
            },
          });
        break;

      case ReportType.Review:
        this.router.navigate(['/admin-users'], { queryParams: { reviewId: this.detail.targetId } });
        break;

      case ReportType.Message:
        this.router.navigate(['/admin-supports'], { queryParams: { messageId: this.detail.targetId } });
        break;
    }
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case ReportStatus.Pending:     return 'badge badge-warning';
      case ReportStatus.UnderReview: return 'badge badge-info';
      case ReportStatus.Resolved:    return 'badge badge-success';
      case ReportStatus.Dismissed:   return 'badge';
      default:                       return 'badge';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case ReportStatus.Pending:     return 'Afventer';
      case ReportStatus.UnderReview: return 'Under gennemgang';
      case ReportStatus.Resolved:    return 'Afgjort';
      case ReportStatus.Dismissed:   return 'Afvist';
      default:                       return status;
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case ReportType.User:    return '👤';
      case ReportType.Item:    return '📦';
      case ReportType.Review:  return '⭐';
      case ReportType.Message: return '💬';
      default:                 return '🚩';
    }
  }

  getTypeBadge(type: string): string {
    switch (type) {
      case ReportType.User:    return 'badge badge-info';
      case ReportType.Item:    return 'badge badge-warning';
      case ReportType.Review:  return 'badge';
      case ReportType.Message: return 'badge badge-success';
      default:                 return 'badge';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case ReportType.User:    return 'Bruger';
      case ReportType.Item:    return 'Annonce';
      case ReportType.Review:  return 'Anmeldelse';
      case ReportType.Message: return 'Besked';
      default:                 return type;
    }
  }

  getReasonLabel(reason: string): string {
    const map: Record<string, string> = {
      FakeIdentity:          'Falsk identitet',
      Scammer:               'Svindler',
      Harassment:            'Chikane',
      InappropriateContent:  'Upassende indhold',
      FakeListing:           'Falsk annonce',
      ProhibitedItem:        'Forbudt genstand',
      MisleadingDescription: 'Vildledende beskrivelse',
      Spam:                  'Spam',
      Other:                 'Andet',
    };
    return map[reason] ?? reason;
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
