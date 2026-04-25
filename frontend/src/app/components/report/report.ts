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
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { ReportService } from '../../services/reportService';
import { ReportDto, ReportListDto } from '../../dtos/reportDto';
import { ReportFilter } from '../../dtos/filterDto';
import { ReportReason, ReportStatus, ReportType } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabId = 'all' | 'pending' | 'resolved' | 'dismissed';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  status?: ReportStatus;
  count?: number;
}

@Component({
  selector: 'app-report',
  imports: [CommonModule, FormsModule],
  templateUrl: './report.html',
  styleUrl: './report.css',
})
export class Report implements OnInit, OnChanges, OnDestroy {

  @Input() openReportId: number | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadReports(); };

  // Expose enum to template
  ReportStatus = ReportStatus;

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',       label: 'All',       icon: '▤' },
    { id: 'pending',   label: 'Pending',   icon: '⏳', status: ReportStatus.Pending },
    { id: 'resolved',  label: 'Resolved',  icon: '✓',  status: ReportStatus.Resolved },
    { id: 'dismissed', label: 'Dismissed', icon: '✕',  status: ReportStatus.Dismissed },
  ];
  activeTab: TabId = 'all';

  // List state
  reports: ReportListDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  totalCount = 0;
  searchQuery = '';
  sortFilter = 'newest';

  // Detail state
  selectedId: number | null = null;
  selectedReport: ReportDto | null = null;
  detailLoading = false;

  constructor(
    private reportService: ReportService,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Dynamic page size (height-driven like dispute) ───────────────────────────
  get pageSize(): number {
    const availableHeight = window.innerHeight - 64 - 52 - 48 - 80;
    return Math.max(5, Math.floor(availableHeight / 110));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadReports();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadReports();
    });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['openReportId'] && this.openReportId) {
      this.openReport(this.openReportId);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────────

  loadReports(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find(t => t.id === this.activeTab)?.status ?? undefined;

    const filter: ReportFilter = {
      search: this.searchQuery.trim() || null,
      status: status ?? null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: Math.max(this.pageSize, 1),
      sortBy: 'createdAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    this.reportService.getMy(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.reports = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find(t => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load reports.';
          }
        },
        error: () => { this.listError = 'An error occurred. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const req: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    // All count
    this.reportService.getMy({}, req).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const tab = this.tabs.find(t => t.id === 'all');
        if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
      }
    });

    // Status counts
    const statusTabs: { id: TabId; status: ReportStatus }[] = [
      { id: 'pending',   status: ReportStatus.Pending },
      { id: 'resolved',  status: ReportStatus.Resolved },
      { id: 'dismissed', status: ReportStatus.Dismissed },
    ];

    for (const { id, status } of statusTabs) {
      this.reportService.getMy({ status }, req).pipe(takeUntil(this.destroy$)).subscribe({
        next: (res) => {
          const tab = this.tabs.find(t => t.id === id);
          if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
        }
      });
    }
  }

  openReport(id: number): void {
    if (this.selectedId === id) return;
    this.selectedId = id;
    this.selectedReport = null;
    this.detailLoading = true;
    this.cdr.markForCheck();

    this.reportService.getById(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.detailLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedReport = res.data;
          }
        },
        error: () => {
          this.selectedId = null;
          this.listError = 'Failed to load report details.';
          this.cdr.markForCheck();
        },
      });
  }

  // ─── Tabs & filters ───────────────────────────────────────────────────────────

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadReports();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadReports();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadReports();
  }

  trackById(_: number, r: ReportListDto): number { return r.id; }

  // ─── UI helpers ───────────────────────────────────────────────────────────────

  getStatusClass(status: ReportStatus | string): string {
    switch (status) {
      case ReportStatus.Pending:   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case ReportStatus.Resolved:  return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case ReportStatus.Dismissed: return 'bg-zinc-700 text-zinc-400 border-zinc-700';
      default:                     return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getTypeBadgeClass(type: ReportType | string): string {
    switch (type) {
      case ReportType.User: return 'bg-blue-400/10 text-blue-400';
      case ReportType.Item: return 'bg-purple-400/10 text-purple-400';
      default:              return 'bg-zinc-800 text-zinc-400';
    }
  }

  getReasonLabel(reason: ReportReason | string): string {
    switch (reason) {
      case ReportReason.FakeIdentity:          return 'Fake Identity';
      case ReportReason.Scammer:               return 'Scammer';
      case ReportReason.Harassment:            return 'Harassment';
      case ReportReason.InappropriateContent:  return 'Inappropriate Content';
      case ReportReason.Spam:                  return 'Spam';
      case ReportReason.Other:                 return 'Other';
      default:                                 return reason as string;
    }
  }

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }
}