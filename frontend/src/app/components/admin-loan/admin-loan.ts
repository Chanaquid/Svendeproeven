import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { LoanDto, LoanListDto } from '../../dtos/loanDto';
import { AuthService } from '../../services/authService';
import { LoanService } from '../../services/loanService';
import { AdminService } from '../../services/adminService';
import { LoanFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { LoanStatus } from '../../dtos/enums';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'adminPending' | 'approved' | 'active' | 'late' | 'completed' | 'cancelled';

@Component({
  selector: 'app-admin-loan',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './admin-loan.html',
  styleUrl: './admin-loan.css',
})
export class AdminLoan implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadLoans(); };

  allLoans: LoanListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'adminPending';

  currentPage = 1;
  totalCount = 0;

  showLoanModal = false;
  isLoadingDetail = false;
  selectedLoan: LoanListDto | null = null;
  loanDetail: LoanDto | null = null;
  selectedPhoto: string | null = null;

  adminDecisionNote = '';
  decisionError = '';
  decisionSuccess = '';
  isDeciding = false;

  showForceCancelConfirm = false;
  forceCancelReason = '';
  isForceCancelling = false;
  forceCancelError = '';

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',          label: 'Alle' },
    { key: 'adminPending', label: 'Admin afventer' },
    { key: 'approved',     label: 'Godkendte' },
    { key: 'active',       label: 'Aktive' },
    { key: 'late',         label: 'Forsinkede' },
    { key: 'completed',    label: 'Gennemført' },
    { key: 'cancelled',    label: 'Annulleret' },
  ];

  constructor(
    private authService: AuthService,
    private loanService: LoanService,
    private adminService: AdminService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 84));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadLoans();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadLoans();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadLoans(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, LoanStatus>> = {
      adminPending: LoanStatus.AdminPending,
      approved:     LoanStatus.Approved,
      active:       LoanStatus.Active,
      late:         LoanStatus.Late,
      completed:    LoanStatus.Completed,
      cancelled:    LoanStatus.Cancelled,
    };

    const filter: LoanFilter = {
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      search: this.searchQuery.trim() || null,
      isOverdue: this.activeTab === 'late' ? true : null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.loanService
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
          this.allLoans = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente lån. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.loanService
      .adminGetAll({}, request)
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

    const statusTabs: { key: TabKey; status: LoanStatus; isOverdue?: boolean }[] = [
      { key: 'adminPending', status: LoanStatus.AdminPending },
      { key: 'approved',     status: LoanStatus.Approved },
      { key: 'active',       status: LoanStatus.Active },
      { key: 'late',         status: LoanStatus.Late, isOverdue: true },
      { key: 'completed',    status: LoanStatus.Completed },
      { key: 'cancelled',    status: LoanStatus.Cancelled },
    ];

    for (const { key, status, isOverdue } of statusTabs) {
      const f: LoanFilter = { status, isOverdue: isOverdue ?? null };
      this.loanService
        .adminGetAll(f, request)
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
    this.loadLoans();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadLoans();
  }

  openLoanModal(loan: LoanListDto): void {
    this.selectedLoan = loan;
    this.loanDetail = null;
    this.showLoanModal = true;
    this.isLoadingDetail = true;
    this.adminDecisionNote = '';
    this.decisionError = '';
    this.decisionSuccess = '';
    this.forceCancelReason = '';
    this.forceCancelError = '';
    this.showForceCancelConfirm = false;
    this.selectedPhoto = null;

    this.loanService
      .adminGetById(loan.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoadingDetail = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => { this.loanDetail = res.data ?? null; },
        error: () => { this.showLoanModal = false; },
      });
  }

  closeModal(): void {
    this.showLoanModal = false;
    this.showForceCancelConfirm = false;
  }

  get requiresAdminApproval(): boolean {
    return this.loanDetail?.status === LoanStatus.AdminPending;
  }

  adminDecide(isApproved: boolean): void {
    if (!this.loanDetail) return;
    this.isDeciding = true;
    this.decisionError = '';

    this.loanService
      .adminReview(this.loanDetail.id, {
        loanId: this.loanDetail.id,
        isApproved,
        adminNote: this.adminDecisionNote.trim() || undefined,
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isDeciding = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.loanDetail = res.data!;
          this.decisionSuccess = isApproved
            ? 'Lån godkendt — videresendt til ejer.'
            : 'Lån afvist.';
          this.loadLoans();
          this.loadTabCounts();
          setTimeout(() => {
            this.showLoanModal = false;
            this.decisionSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.decisionError = err.error?.message ?? 'Kunne ikke behandle afgørelsen.';
        },
      });
  }

  get canForceCancelLoan(): boolean {
    const s = this.loanDetail?.status;
    return s === LoanStatus.AdminPending || s === LoanStatus.Approved || s === LoanStatus.Active;
  }

  forceCancel(): void {
    if (!this.loanDetail || !this.forceCancelReason.trim()) return;
    this.isForceCancelling = true;
    this.forceCancelError = '';

    this.adminService
      .forceCancelLoan(this.loanDetail.id, this.forceCancelReason.trim())
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isForceCancelling = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.showForceCancelConfirm = false;
          this.showLoanModal = false;
          this.loadLoans();
          this.loadTabCounts();
        },
        error: (err) => {
          this.forceCancelError = err.error?.message ?? 'Kunne ikke annullere lån.';
        },
      });
  }

  getDaysOverdue(loan: LoanDto | LoanListDto): number {
    if (!loan.isOverdue || !loan.endDate) return 0;
    return Math.max(
      0,
      Math.floor((new Date().getTime() - new Date(loan.endDate).getTime()) / (1000 * 60 * 60 * 24)),
    );
  }

  getStatusBadge(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':       return 'badge badge-success';
      case 'approved':     return 'badge badge-info';
      case 'completed':    return 'badge badge-success';
      case 'late':         return 'badge badge-danger';
      case 'adminpending': return 'badge badge-warning';
      case 'cancelled':
      case 'rejected':     return 'badge';
      default:             return 'badge';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active':       return 'Aktiv';
      case 'Approved':     return 'Godkendt';
      case 'Pending':      return 'Afventer';
      case 'AdminPending': return 'Admin afventer';
      case 'Late':         return 'Forsinket';
      case 'Completed':    return 'Gennemført';
      case 'Cancelled':    return 'Annulleret';
      case 'Rejected':     return 'Afvist';
      default:             return status;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
