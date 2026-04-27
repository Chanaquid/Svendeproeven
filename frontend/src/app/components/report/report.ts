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

  ReportStatus = ReportStatus;
  ReportType = ReportType;

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',       label: 'Alle',     icon: '▤' },
    { id: 'pending',   label: 'Afventer', icon: '⏳', status: ReportStatus.Pending },
    { id: 'resolved',  label: 'Afgjort',  icon: '✓',  status: ReportStatus.Resolved },
    { id: 'dismissed', label: 'Afvist',   icon: '✕',  status: ReportStatus.Dismissed },
  ];
  activeTab: TabId = 'all';

  reports: ReportListDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  totalCount = 0;
  searchQuery = '';
  sortFilter = 'newest';

  selectedId: number | null = null;
  selectedReport: ReportDto | null = null;
  detailLoading = false;

  constructor(
    private reportService: ReportService,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const availableHeight = window.innerHeight - 64 - 52 - 48 - 80;
    return Math.max(5, Math.floor(availableHeight / 110));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
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

  loadReports(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find((t) => t.id === this.activeTab)?.status ?? undefined;

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

    this.reportService
      .getMy(filter, request)
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
            this.reports = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find((t) => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Kunne ikke hente anmeldelser.';
          }
        },
        error: () => {
          this.listError = 'Der opstod en fejl. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const req: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.reportService
      .getMy({}, req)
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

    const statusTabs: { id: TabId; status: ReportStatus }[] = [
      { id: 'pending',   status: ReportStatus.Pending },
      { id: 'resolved',  status: ReportStatus.Resolved },
      { id: 'dismissed', status: ReportStatus.Dismissed },
    ];

    for (const { id, status } of statusTabs) {
      this.reportService
        .getMy({ status }, req)
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

  openReport(id: number): void {
    if (this.selectedId === id) return;
    this.selectedId = id;
    this.selectedReport = null;
    this.detailLoading = true;
    this.cdr.markForCheck();

    this.reportService
      .getById(id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.detailLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) this.selectedReport = res.data;
        },
        error: () => {
          this.selectedId = null;
          this.listError = 'Kunne ikke indlæse anmeldelse.';
          this.cdr.markForCheck();
        },
      });
  }

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

  getStatusBadge(status: ReportStatus | string): string {
    switch (status) {
      case ReportStatus.Pending:   return 'badge badge-warning';
      case ReportStatus.Resolved:  return 'badge badge-success';
      case ReportStatus.Dismissed: return 'badge';
      default:                     return 'badge';
    }
  }

  getStatusLabel(status: ReportStatus | string): string {
    switch (status) {
      case ReportStatus.Pending:   return 'Afventer';
      case ReportStatus.Resolved:  return 'Afgjort';
      case ReportStatus.Dismissed: return 'Afvist';
      default:                     return status as string;
    }
  }

  getTypeBadge(type: ReportType | string): string {
    switch (type) {
      case ReportType.User: return 'badge badge-info';
      case ReportType.Item: return 'badge badge-warning';
      default:              return 'badge';
    }
  }

  getTypeLabel(type: ReportType | string): string {
    switch (type) {
      case ReportType.User: return 'Bruger';
      case ReportType.Item: return 'Annonce';
      default:              return type as string;
    }
  }

  getReasonLabel(reason: ReportReason | string): string {
    switch (reason) {
      case ReportReason.FakeIdentity:         return 'Falsk identitet';
      case ReportReason.Scammer:              return 'Svindler';
      case ReportReason.Harassment:           return 'Chikane';
      case ReportReason.InappropriateContent: return 'Upassende indhold';
      case ReportReason.Spam:                 return 'Spam';
      case ReportReason.Other:                return 'Andet';
      default:                                return reason as string;
    }
  }
}
