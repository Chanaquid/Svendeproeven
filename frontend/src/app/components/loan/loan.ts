import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { LoanService } from '../../services/loanService';
import { LoanListDto } from '../../dtos/loanDto';
import { forkJoin } from 'rxjs';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

@Component({
  selector: 'app-loan',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
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
  activeTab: 'all' | 'active' | 'approved' | 'pending' | 'late' | 'completed' | 'cancelled' | 'rejected' = 'all';
  sortLabel = 'newest';

  // --- Fixed Pagination ---
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
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    
    this.route.queryParams.subscribe(params => {
      this.loanView = params['view'] || 'borrowed';
      this.activeTab = params['tab'] || 'all';
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
    // Fetching enough records to handle local pagination/sorting for now
    forkJoin({
      borrowed: this.loanService.getMyAsBorrower({}, { page: 1, pageSize: 1000 }),
      owned: this.loanService.getMyAsLender({}, { page: 1, pageSize: 1000 })
    }).subscribe({
      next: (res) => {
        this.borrowedLoans = res.borrowed.data?.items ?? [];
        this.ownedLoans = res.owned.data?.items ?? [];
        this.isLoading = false;
        this.applyFilters();
      },
      error: () => this.isLoading = false
    });
  }

  applyFilters(): void {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    let result = [...source];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(l => l.itemTitle.toLowerCase().includes(q) || l.otherPartyName.toLowerCase().includes(q));
    }

    if (this.activeTab !== 'all') {
      result = result.filter(l => {
        if (this.activeTab === 'pending') return l.status === 'Pending' || l.status === 'AdminPending';
        return l.status.toLowerCase() === this.activeTab.toLowerCase();
      });
    }

    switch (this.sortLabel) {
      case 'newest': result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()); break;
      case 'oldest': result.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()); break;
      case 'price_high': result.sort((a, b) => b.totalPrice - a.totalPrice); break;
      case 'price_low': result.sort((a, b) => a.totalPrice - b.totalPrice); break;
    }

    this.filteredLoans = result;
    this.cdr.detectChanges();
  }

  onTabChange(key: any) { this.activeTab = key; this.currentPage = 1; this.applyFilters(); this.syncUrl(); }
  onViewChange(view: any) { this.loanView = view; this.currentPage = 1; this.applyFilters(); this.syncUrl(); }
  onSortChange(val: string) { this.sortLabel = val; this.currentPage = 1; this.applyFilters(); this.syncUrl(); }
  
  private syncUrl() {
    this.router.navigate([], { relativeTo: this.route, queryParams: { view: this.loanView, tab: this.activeTab, page: this.currentPage }, queryParamsHandling: 'merge' });
  }

  getTabCount(key: string): number {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    if (key === 'all') return source.length;
    if (key === 'pending') return source.filter(l => l.status === 'Pending' || l.status === 'AdminPending').length;
    return source.filter(l => l.status.toLowerCase() === key.toLowerCase()).length;
  }

  get activeCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Active').length; }
  get approvedCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Approved').length; }
  get pendingCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Pending' || l.status === 'AdminPending').length; }
  get rejectedCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Rejected').length; }
  get lateCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Late').length; }
  get completedCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Completed').length; }
  get cancelledCount() { return [...this.borrowedLoans, ...this.ownedLoans].filter(l => l.status === 'Cancelled').length; }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) || '??';
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-sky-400/10 text-sky-400';
      case 'approved': return 'bg-blue-400/10 text-blue-400';
      case 'completed': return 'bg-emerald-400/10 text-emerald-400';
      case 'late': return 'bg-orange-500/10 text-orange-500';
      case 'pending': return 'bg-yellow-500/10 text-yellow-500';
      case 'adminpending': return 'bg-amber-400/10 text-amber-400';
      case 'cancelled': return 'bg-zinc-500/10 text-zinc-500';
      case 'rejected': return 'bg-red-500/10 text-red-500';
      default: return 'bg-zinc-800 text-zinc-400';
    }
  }

  goToLoan(id: number) { this.router.navigate(['/loans', id]); }
  goToPage(p: number) { this.currentPage = p; this.syncUrl(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
}