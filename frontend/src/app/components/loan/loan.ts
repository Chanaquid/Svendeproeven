import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { LoanService } from '../../services/loanService';
import { LoanListDto } from '../../dtos/loanDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

@Component({
  selector: 'app-loan',
  standalone: true,
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './loan.html',
  styleUrl: './loan.css',
})
export class Loan implements OnInit {
  borrowedLoans: LoanListDto[] = [];
  ownedLoans: LoanListDto[] = [];
  filteredLoans: LoanListDto[] = [];

  isLoading = true;
  searchQuery = '';
  loanView: 'borrowed' | 'lent' = 'borrowed';
  activeTab:
    | 'all'
    | 'active'
    | 'approved'
    | 'pending'
    | 'late'
    | 'completed'
    | 'cancelled'
    | 'rejected' = 'all';
  sortLabel = 'newest';

  currentPage = 1;
  readonly PAGE_SIZE = 10;

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'active' as const, label: 'Active' },
    { key: 'approved' as const, label: 'Approved' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'late' as const, label: 'Late' },
    { key: 'completed' as const, label: 'Completed' },
    { key: 'cancelled' as const, label: 'Cancelled' },
    { key: 'rejected' as const, label: 'Rejected' },
  ];

  constructor(
    private authService: AuthService,
    private loanService: LoanService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.subscribe((params) => {
      this.loanView = params['view'] || 'borrowed';
      this.activeTab = params['tab'] || 'all';
      this.currentPage = +params['page'] || 1;
    });

    this.loadLoans();
  }

  // ─── Pagination ──────────────────────────────────────────────────────
  get pagedItems(): LoanListDto[] {
    const start = (this.currentPage - 1) * this.PAGE_SIZE;
    return this.filteredLoans.slice(start, start + this.PAGE_SIZE);
  }

  get totalPages(): number {
    return getTotalPages(this.filteredLoans.length, this.PAGE_SIZE);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  // ─── Data loading ────────────────────────────────────────────────────
  private loadLoans(): void {
    this.isLoading = true;
    forkJoin({
      borrowed: this.loanService.getMyAsBorrower({}, { page: 1, pageSize: 1000 }),
      owned: this.loanService.getMyAsLender({}, { page: 1, pageSize: 1000 }),
    }).subscribe({
      next: (res) => {
        this.borrowedLoans = res.borrowed.data?.items ?? [];
        this.ownedLoans = res.owned.data?.items ?? [];
        this.isLoading = false;
        this.applyFilters();
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  // ─── Filters + sort ──────────────────────────────────────────────────
  applyFilters(): void {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    let result = [...source];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(
        (l) => l.itemTitle.toLowerCase().includes(q) || l.otherPartyName.toLowerCase().includes(q),
      );
    }

    if (this.activeTab !== 'all') {
      result = result.filter((l) => {
        if (this.activeTab === 'pending') {
          return l.status === 'Pending' || l.status === 'AdminPending';
        }
        return l.status.toLowerCase() === this.activeTab.toLowerCase();
      });
    }

    switch (this.sortLabel) {
      case 'newest':
        result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        break;
      case 'oldest':
        result.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
        break;
      case 'price_high':
        result.sort((a, b) => b.totalPrice - a.totalPrice);
        break;
      case 'price_low':
        result.sort((a, b) => a.totalPrice - b.totalPrice);
        break;
    }

    this.filteredLoans = result;
    this.cdr.detectChanges();
  }

  onTabChange(key: any) {
    this.activeTab = key;
    this.currentPage = 1;
    this.applyFilters();
    this.syncUrl();
  }
  onViewChange(view: any) {
    this.loanView = view;
    this.currentPage = 1;
    this.applyFilters();
    this.syncUrl();
  }
  onSortChange(val: string) {
    this.sortLabel = val;
    this.currentPage = 1;
    this.applyFilters();
    this.syncUrl();
  }

  private syncUrl() {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        view: this.loanView,
        tab: this.activeTab,
        page: this.currentPage,
      },
      queryParamsHandling: 'merge',
    });
  }

  // ─── Counters ────────────────────────────────────────────────────────
  getTabCount(key: string): number {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    if (key === 'all') return source.length;
    if (key === 'pending') {
      return source.filter((l) => l.status === 'Pending' || l.status === 'AdminPending').length;
    }
    return source.filter((l) => l.status.toLowerCase() === key.toLowerCase()).length;
  }

  private get allLoans(): LoanListDto[] {
    return [...this.borrowedLoans, ...this.ownedLoans];
  }

  get activeCount() {
    return this.allLoans.filter((l) => l.status === 'Active').length;
  }
  get approvedCount() {
    return this.allLoans.filter((l) => l.status === 'Approved').length;
  }
  get pendingCount() {
    return this.allLoans.filter((l) => l.status === 'Pending' || l.status === 'AdminPending')
      .length;
  }
  get rejectedCount() {
    return this.allLoans.filter((l) => l.status === 'Rejected').length;
  }
  get lateCount() {
    return this.allLoans.filter((l) => l.status === 'Late').length;
  }
  get completedCount() {
    return this.allLoans.filter((l) => l.status === 'Completed').length;
  }
  get cancelledCount() {
    return this.allLoans.filter((l) => l.status === 'Cancelled').length;
  }

  // ─── Helpers ─────────────────────────────────────────────────────────
  getInitials(name: string): string {
    return (
      name
        ?.split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
        .slice(0, 2) || '??'
    );
  }

  /** Returns a plain CSS class for the status pill. Styled in loan.css. */
  getLoanStatusClass(status: string): string {
    return 'status-loan-' + (status ?? 'unknown').toLowerCase();
  }

  goToLoan(id: number) {
    this.router.navigate(['/loans', id]);
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.syncUrl();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
