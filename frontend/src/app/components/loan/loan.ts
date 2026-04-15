import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth-service';
import { LoanService } from '../../services/loan-service';
import { LoanDTO } from '../../dtos/loanDTO';
import { Navbar } from '../navbar/navbar';
import { LoanStatus } from '../../dtos/enums';

@Component({
  selector: 'app-loan',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './loan.html',
  styleUrl: './loan.css',
})
export class Loan implements OnInit {

  borrowedLoans: LoanDTO.LoanSummaryDTO[] = [];
  ownedLoans: LoanDTO.LoanSummaryDTO[] = [];
  filteredLoans: LoanDTO.LoanSummaryDTO[] = [];

  isLoading = true;
  searchQuery = '';
  loanView: 'borrowed' | 'lent' = 'borrowed';
  activeTab: 'all' | 'active' | 'approved' | 'pending' | 'late' | 'returned' | 'rejected' = 'all';

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'active' as const, label: 'Active' },
    { key: 'approved' as const, label: 'Approved' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'late' as const, label: 'Late' },
    { key: 'returned' as const, label: 'Returned' },
    { key: 'rejected' as const, label: 'Rejected' },
  ];
  constructor(
    private authService: AuthService,
    private loanService: LoanService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadLoans();
  }

  private loadLoans(): void {

    this.isLoading = true;
    
    let borrowedDone = false, 
    ownedDone = false;

    this.loanService.getBorrowedLoans().subscribe({
      next: (loans) => {
        this.borrowedLoans = loans;
        borrowedDone = true;
        
        if (ownedDone) { 
          
          this.isLoading = false; 
          this.applyFilters(); 
        }
        
        this.cdr.detectChanges();
      },
      error: () => {
        borrowedDone = true;
        if (ownedDone) { 
          this.isLoading = false; 
          this.applyFilters(); 
        }
      }
    });

    this.loanService.getOwnedLoans().subscribe({
      next: (loans) => {
        this.ownedLoans = loans;
        ownedDone = true;
        if (borrowedDone) { 
          this.isLoading = false; 
          this.applyFilters(); 
        }
        this.cdr.detectChanges();
      },
      error: () => {
        ownedDone = true;
        if (borrowedDone) { 
          this.isLoading = false; 
          this.applyFilters(); 
        }
      }
    });
  }

  applyFilters(): void {
    
    let result: LoanDTO.LoanSummaryDTO[] = [];

    if (this.loanView === 'borrowed') {
      
      result = [...this.borrowedLoans];
    
    } else {

      result = [...this.ownedLoans];
    }

    if (this.searchQuery.trim() != '') {
      
      const searchText = this.searchQuery.toLowerCase();

      result = result.filter((loan) => {

        return (
          loan.itemTitle.toLowerCase().includes(searchText) ||
          loan.otherPartyName.toLowerCase().includes(searchText)
        );
      });
    }

    switch (this.activeTab) {
      case 'active': result = result.filter(l => l.status === 'Active'); break;
      case 'approved': result = result.filter(l => l.status === 'Approved'); break;
      case 'pending': result = result.filter(l => l.status === 'Pending' || l.status === 'AdminPending'); break;
      case 'late': result = result.filter(l => l.status === 'Late'); break;
      case 'returned': result = result.filter(l => l.status === 'Returned' || l.status === 'Cancelled'); break;
      case 'rejected': result = result.filter(l => l.status === 'Rejected'); break;

    }

    this.filteredLoans = result;
    console.log(this.filteredLoans);
    this.cdr.detectChanges();
  }

  getTabCount(key: string): number {
    const source = this.loanView === 'borrowed' ? this.borrowedLoans : this.ownedLoans;
    switch (key) {
      case 'all': return source.length;
      case 'active': return source.filter(l => l.status === 'Active').length;
      case 'approved': return source.filter(l => l.status === 'Approved').length;
      case 'pending': return source.filter(l => l.status === 'Pending' || l.status === 'AdminPending').length;
      case 'late': return source.filter(l => l.status === 'Late').length;
      case 'returned': return source.filter(l => l.status === 'Returned' || l.status === 'Cancelled').length;
      case 'rejected': return source.filter(l => l.status === 'Rejected').length;

      default: return 0;
    }
  }

  get activeCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Active';
    }).length;
  }

  get lateCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Late';
    }).length;
  }

  get returnedCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Returned';
    }).length;
  }

  get pendingCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Pending' || loan.status === 'AdminPending';
    }).length;
  }

  get approvedCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Approved';
    }).length;
  }

  get rejectedCount(): number {
    const allLoans = [...this.borrowedLoans, ...this.ownedLoans];

    return allLoans.filter((loan) => {
      return loan.status === 'Rejected';
    }).length;
  }

  goToLoan(id: number): void {
    this.router.navigate(['/loans', id]);
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400';
      case 'approved': return 'bg-blue-400/10 text-blue-400';
      case 'returned': return 'bg-cyan-300/10 text-cyan-300';
      case 'late': return 'bg-red-400/10 text-red-400';
      case 'pending':
      case 'adminpending': return 'bg-amber-400/10 text-amber-400';
      case 'cancelled':
      case 'rejected': return 'bg-rose-400/10 text-rose-400';
      default: return 'bg-zinc-800 text-zinc-400';
    }
  }


}
