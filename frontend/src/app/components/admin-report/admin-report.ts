import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { ReportService } from '../../services/reportService';
import { ReportDto, ReportListDto } from '../../dtos/reportDto';
import { ReportFilter } from '../../dtos/filterDto';
import { ReportReason, ReportStatus, ReportType } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import { ItemService } from '../../services/itemService';

type TabKey = 'all' | 'pending' | 'underReview' | 'resolved' | 'dismissed';
type SortKey = 'newest' | 'oldest' | 'user' | 'item' | 'review' | 'message';

@Component({
  selector: 'app-admin-report',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
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


  // Pagination
  currentPage = 1;
  totalCount = 0;

  // Modal
  showModal = false;
  isLoadingDetail = false;
  selectedItem: ReportDto | null = null;
  detail: ReportDto | null = null;

  // Resolve
  resolveStatus: ReportStatus | '' = '';
  resolveNote = '';
  resolveError = '';
  resolveSuccess = '';
  isResolving = false;
  showResolveForm = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',         label: 'All' },
    { key: 'pending',     label: 'Pending' },
    { key: 'underReview', label: 'Under Review' },
    { key: 'resolved',    label: 'Resolved' },
    { key: 'dismissed',   label: 'Dismissed' },
  ];

  sortOptions: { key: SortKey; label: string }[] = [
    { key: 'newest',  label: 'Newest first' },
    { key: 'oldest',  label: 'Oldest first' },
    { key: 'user',    label: 'Type: User' },
    { key: 'item',    label: 'Type: Item' },
    { key: 'review',  label: 'Type: Review' },
    { key: 'message', label: 'Type: Message' },
  ];

  readonly ReportStatus = ReportStatus;
  readonly ReportType   = ReportType;
  readonly ReportReason = ReportReason;

  resolveOptions: { value: ReportStatus; label: string; desc: string }[] = [
    { value: ReportStatus.UnderReview, label: '🔍 Mark Under Review', desc: 'Acknowledge and start reviewing' },
    { value: ReportStatus.Resolved,    label: '✓ Resolve',            desc: 'Action has been taken' },
    { value: ReportStatus.Dismissed,   label: '✕ Dismiss',            desc: 'Not a valid report' },
  ];

  constructor(
    private authService: AuthService,
    private reportService: ReportService,
    private itemService: ItemService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Dynamic page size ────────────────────────────────────────────────────

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

    this.loadReports();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => { this.currentPage = 1; this.loadReports(); });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ────────────────────────────────────────────────────────────────

  loadReports(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, ReportStatus>> = {
      pending:     ReportStatus.Pending,
      underReview: ReportStatus.UnderReview,
      resolved:    ReportStatus.Resolved,
      dismissed:   ReportStatus.Dismissed,
    };

    // Type filter from sort key
    const typeFilterMap: Partial<Record<SortKey, ReportType>> = {
      user:    ReportType.User,
      item:    ReportType.Item,
      review:  ReportType.Review,
      message: ReportType.Message,
    };

    const filter: ReportFilter = {
      status: this.activeTab !== 'all' ? (statusMap[this.activeTab] ?? null) : null,
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

    this.reportService.adminGetAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (res) => {
          this.reports = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find(t => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => { this.listError = 'Failed to load reports. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.reportService.adminGetAll(null, request).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const tab = this.tabs.find(t => t.key === 'all');
        if (tab) { tab.count = res.data?.totalCount ?? 0; this.cdr.detectChanges(); }
      }
    });

    const statusTabs: { key: TabKey; status: ReportStatus }[] = [
      { key: 'pending',     status: ReportStatus.Pending },
      { key: 'underReview', status: ReportStatus.UnderReview },
      { key: 'resolved',    status: ReportStatus.Resolved },
      { key: 'dismissed',   status: ReportStatus.Dismissed },
    ];

    for (const { key, status } of statusTabs) {
      this.reportService.adminGetAll({ status }, request).pipe(takeUntil(this.destroy$)).subscribe({
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

  // ─── Modal ───────────────────────────────────────────────────────────────

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

    this.reportService.getById(item.id)
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
    this.targetError = '';
  }

  // ─── Resolve ─────────────────────────────────────────────────────────────

  get canResolve(): boolean {
    return this.detail?.status === ReportStatus.Pending
        || this.detail?.status === ReportStatus.UnderReview;
  }

  resolve(): void {
    if (!this.detail || !this.resolveStatus) return;
    this.isResolving = true;
    this.resolveError = '';

    this.reportService.adminResolve(this.detail.id, {
      status: this.resolveStatus as ReportStatus,
      adminNote: this.resolveNote.trim() || undefined,
    }).pipe(takeUntil(this.destroy$), finalize(() => {
      this.isResolving = false;
      this.cdr.detectChanges();
    })).subscribe({
      next: (res) => {
        this.detail = res.data!;
        this.resolveSuccess = 'Report updated successfully.';
        this.showResolveForm = false;
        this.loadReports();
        this.loadTabCounts();
        setTimeout(() => {
          this.showModal = false;
          this.resolveSuccess = '';
          this.cdr.detectChanges();
        }, 1500);
      },
      error: (err) => { this.resolveError = err.error?.message ?? 'Failed to update report.'; },
    });
  }

  // ─── Navigate to target ───────────────────────────────────────────────────

 navigateToTarget(): void {
    if (!this.detail) return;

    this.targetError = '';

    //Consolidate logic into a single switch to prevent double navigation
    switch (this.detail.type) {
      case ReportType.User:
        this.router.navigate(['/users', this.detail.targetId]);
        break;

      case ReportType.Item:
        this.isLoadingTarget = true;
        this.itemService.getById(Number(this.detail.targetId))
          .pipe(
            takeUntil(this.destroy$),
            finalize(() => {
              this.isLoadingTarget = false;
              this.cdr.detectChanges();
            })
          )
          .subscribe({
            next: (res) => {
              // Navigate using the slug from the fresh item data
              if (res.data?.slug) {
                this.router.navigate(['/items', res.data.slug]);
              } else {
                this.targetError = 'This item no longer exists.';
              }
            },
            error: (err) => {
            this.targetError = 'This item has been deleted or is no longer available.';
                          this.cdr.detectChanges();
            }
          });
        break;

      case ReportType.Review:
        this.router.navigate(['/admin-users'], { 
          queryParams: { reviewId: this.detail.targetId } 
        });
        break;

      case ReportType.Message:
        this.router.navigate(['/admin-supports'], { 
          queryParams: { messageId: this.detail.targetId } 
        });
        break;
        
      default:
        console.warn('Unknown report type:', this.detail.type);
        break;
    }
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  getStatusClass(status: string): string {
    switch (status) {
      case ReportStatus.Pending:     return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case ReportStatus.UnderReview: return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case ReportStatus.Resolved:    return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case ReportStatus.Dismissed:   return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      default:                       return 'bg-zinc-800 text-zinc-400 border-zinc-700';
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

  getTypeClass(type: string): string {
    switch (type) {
      case ReportType.User:    return 'bg-purple-400/10 text-purple-400';
      case ReportType.Item:    return 'bg-blue-400/10 text-blue-400';
      case ReportType.Review:  return 'bg-amber-400/10 text-amber-400';
      case ReportType.Message: return 'bg-teal-400/10 text-teal-400';
      default:                 return 'bg-zinc-800 text-zinc-400';
    }
  }

  getReasonLabel(reason: string): string {
    const map: Record<string, string> = {
      FakeIdentity:          'Fake Identity',
      Scammer:               'Scammer',
      Harassment:            'Harassment',
      InappropriateContent:  'Inappropriate Content',
      FakeListing:           'Fake Listing',
      ProhibitedItem:        'Prohibited Item',
      MisleadingDescription: 'Misleading Description',
      Spam:                  'Spam',
      Other:                 'Other',
    };
    return map[reason] ?? reason;
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}