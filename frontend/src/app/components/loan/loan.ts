import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { LoanService } from '../../services/loanService';
import { LoanListDto } from '../../dtos/loanDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey =
  | 'all' | 'active' | 'approved' | 'pending'
  | 'late' | 'completed' | 'cancelled' | 'rejected';

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
  activeTab: TabKey = 'all';
  sortLabel = 'newest';

  currentPage = 1;
  readonly PAGE_SIZE = 10;

  tabs: { key: TabKey; label: string }[] = [
    { key: 'all',       label: 'Alle' },
    { key: 'active',    label: 'Aktive' },
    { key: 'approved',  label: 'Godkendte' },
    { key: 'pending',   label: 'Afventer' },
    { key: 'late',      label: 'Forsinkede' },
    { key: 'completed', label: 'Gennemført' },
    { key: 'cancelled', label: 'Annulleret' },
    { key: 'rejected',  label: 'Afvist' },
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
      this.activeTab = (params['tab'] as TabKey) || 'all';
      this.currentPage = +params['page'] || 1;
    });

    this.loadLoans();
  }

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
      error: () => (this.isLoading = false),
    });
  }

  applyFilters(): void {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    let result = [...source];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(
        (l) =>
          l.itemTitle.toLowerCase().includes(q) ||
          l.otherPartyName.toLowerCase().includes(q),
      );
    }

    if (this.activeTab !== 'all') {
      result = result.filter((l) => {
        if (this.activeTab === 'pending')
          return l.status === 'Pending' || l.status === 'AdminPending';
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

  onTabChange(key: TabKey) {
    this.activeTab = key;
    this.currentPage = 1;
    this.applyFilters();
    this.syncUrl();
  }
  onViewChange(view: 'borrowed' | 'lent') {
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
  onSearch() {
    this.currentPage = 1;
    this.applyFilters();
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

  getTabCount(key: string): number {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    if (key === 'all') return source.length;
    if (key === 'pending')
      return source.filter((l) => l.status === 'Pending' || l.status === 'AdminPending').length;
    return source.filter((l) => l.status.toLowerCase() === key.toLowerCase()).length;
  }

  /**
   * Returns a BEM-style modifier class for the loan status badge.
   * Maps to the status-chip variants in styles.css (.badge-* tokens).
   */
  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':
      case 'approved':
        return 'badge-info';
      case 'completed':
        return 'badge-success';
      case 'late':
      case 'rejected':
        return 'badge-danger';
      case 'pending':
      case 'adminpending':
        return 'badge-warning';
      case 'cancelled':
      default:
        return 'badge';
    }
  }

  /** Translate raw backend status into a Danish label */
  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active': return 'Aktiv';
      case 'Approved': return 'Godkendt';
      case 'Pending': return 'Afventer';
      case 'AdminPending': return 'Afventer admin';
      case 'Late': return 'Forsinket';
      case 'Completed': return 'Gennemført';
      case 'Cancelled': return 'Annulleret';
      case 'Rejected': return 'Afvist';
      default: return status;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) || '??';
  }

  goToLoan(id: number) {
    this.router.navigate(['/loans', id]);
  }

  goToPage(p: number) {
    this.currentPage = p;
    this.syncUrl();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
