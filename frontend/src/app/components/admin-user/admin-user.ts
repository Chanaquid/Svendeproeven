import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AdminEditUserDto, AdminUserDto } from '../../dtos/adminUserDto';
import { ScoreHistoryDto } from '../../dtos/scoreHistoryDto';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { UserFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { ScoreChangeReason } from '../../dtos/enums';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

@Component({
  selector: 'app-admin-user',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-user.html',
  styleUrl: './admin-user.css',
})
export class AdminUser implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  allUsers: AdminUserDto[] = [];
  filteredUsers: AdminUserDto[] = [];
  isLoading = true;
  searchQuery = '';
  activeTab: 'all' | 'verified' | 'unverified' | 'admin' | 'deleted' = 'all';

  // Pagination state
  currentPage = 1;
  pageSize = 50;
  totalCount = 0;

  // Modal & Detailed State
  showUserModal = false;
  isLoadingDetail = false;
  selectedUser: AdminUserDto | null = null;
  // Combined type to satisfy template's need for scoreHistory
  userDetail: (AdminUserDto & { scoreHistory?: ScoreHistoryDto[] }) | null = null;

  // Score adjust
  scoreAdjustment: number | null = null;
  scoreNote = '';
  isAdjustingScore = false;
  scoreError = '';
  scoreSuccess = '';

  // UI state for collapses and visibility
  visibleScoreHistory = 5;
  visibleFines = 5;
  visibleLoans = 5;
  visibleItems = 5;
  finesCollapsed = false;
  loansCollapsed = false;
  itemsCollapsed = false;
  scoreHistoryCollapsed = false;

  // Forms
  showEditForm = false;
  editForm: AdminEditUserDto | null = null;
  isSavingEdit = false;
  editError = '';
  editSuccess = '';

  // Delete
  showDeleteConfirm = false;
  isDeletingUser = false;
  deleteWarnings: string[] = [];
  deletionNote = '';

  // Generic Actions
  actionError = '';
  actionSuccess = '';

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'verified' as const, label: 'Verified' },
    { key: 'unverified' as const, label: 'Unverified' },
    { key: 'admin' as const, label: 'Admins' },
    { key: 'deleted' as const, label: 'Deleted' },
  ];

  constructor(
    private authService: AuthService,
    private userService: UserService,
    public router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── COUNTS & TAB HELPERS ──────────────────────────────────────────────────

  get verifiedCount()   { return this.allUsers.filter(u => u.isVerified && !u.isDeleted).length; }
  get unverifiedCount() { return this.allUsers.filter(u => !u.isVerified && !u.isDeleted).length; }
  get adminCount()      { return this.allUsers.filter(u => u.role === 'Admin' && !u.isDeleted).length; }
  get deletedCount()    { return this.allUsers.filter(u => u.isDeleted).length; }

  getTabCount(key: string): number {
    switch (key) {
      case 'all':        return this.allUsers.filter(u => !u.isDeleted).length;
      case 'verified':   return this.verifiedCount;
      case 'unverified': return this.unverifiedCount;
      case 'admin':      return this.adminCount;
      case 'deleted':    return this.deletedCount;
      default:           return 0;
    }
  }

  // ─── DATA LOADING ──────────────────────────────────────────────────────────

  loadUsers(): void {
    this.isLoading = true;
    
    const filter: UserFilter = {
      search: this.searchQuery.trim() || null
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'CreatedAt',
      sortDescending: true
    };

    this.userService.getAllIncludingDeleted(filter, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.allUsers = res.data.items;
            this.totalCount = res.data.totalCount;
            console.log(res)
            this.applyFilters();
          }
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isLoading = false;
          this.actionError = err.error?.message ?? 'Failed to load users.';
          this.cdr.detectChanges();
        }
      });
  }

  applyFilters(): void {
    let result = [...this.allUsers];

    switch (this.activeTab) {
      case 'verified':   result = result.filter(u => u.isVerified && !u.isDeleted); break;
      case 'unverified': result = result.filter(u => !u.isVerified && !u.isDeleted); break;
      case 'admin':      result = result.filter(u => u.role === 'Admin' && !u.isDeleted); break;
      case 'deleted':    result = result.filter(u => u.isDeleted); break;
      default:           result = result.filter(u => !u.isDeleted); break;
    }

    this.filteredUsers = result;
    this.cdr.detectChanges();
  }

  setTab(tab: 'all' | 'verified' | 'unverified' | 'admin' | 'deleted') {
    this.activeTab = tab;
    this.applyFilters();
  }

  // ─── USER DETAILS & MODAL ──────────────────────────────────────────────────

  openUserModal(user: AdminUserDto): void {
    this.selectedUser = user;
    this.userDetail = null;
    this.showUserModal = true;
    this.isLoadingDetail = true;
    this.showEditForm = false;
    this.scoreAdjustment = null;
    this.scoreNote = '';
    
    this.userService.getUserById(user.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.data) {
            this.userDetail = res.data;
            this.loadScoreHistory(user.id);
          }
          this.isLoadingDetail = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isLoadingDetail = false;
          this.actionError = err.error?.message ?? 'Failed to load user details.';
          this.cdr.detectChanges();
        }
      });
  }

  private loadScoreHistory(userId: string): void {
    this.userService.getUserScoreHistory(userId, {}, { page: 1, pageSize: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe(res => {
        if (this.userDetail && res.data) {
          this.userDetail.scoreHistory = res.data.items;
          this.cdr.detectChanges();
        }
      });
  }

  // ─── ACTIONS ───────────────────────────────────────────────────────────────

  adjustScore(): void {
    if (!this.userDetail || this.scoreAdjustment === null) return;
    this.isAdjustingScore = true;
    this.scoreError = '';

    this.userService.adjustScore(this.userDetail.id, {
      pointsChanged: this.scoreAdjustment,
      note: this.scoreNote.trim() || 'Manual adjustment',
      reason: ScoreChangeReason.AdminAdjustment
    }).subscribe({
      next: () => {
        this.isAdjustingScore = false;
        this.scoreSuccess = `Score adjusted successfully.`;
        this.scoreAdjustment = null;
        this.scoreNote = '';
        
        this.loadScoreHistory(this.userDetail!.id);
        
        // Update local state for immediate feedback in list
        const idx = this.allUsers.findIndex(u => u.id === this.userDetail!.id);
        if (idx !== -1) {
            this.userService.getUserById(this.userDetail!.id).subscribe(res => {
                if (res.data) {
                    this.userDetail!.score = res.data.score;
                    this.allUsers[idx].score = res.data.score;
                    this.cdr.detectChanges();
                }
            });
        }
        
        setTimeout(() => this.scoreSuccess = '', 3000);
      },
      error: (err) => {
        this.scoreError = err.error?.message ?? 'Failed to adjust score.';
        this.isAdjustingScore = false;
        this.cdr.detectChanges();
      }
    });
  }

  openEdit(): void {
    if (!this.userDetail) return;
    this.editForm = {
      fullName: this.userDetail.fullName,
      username: this.userDetail.username,
      email: this.userDetail.email,
      avatarUrl: this.userDetail.avatarUrl,
      gender: this.userDetail.gender,
      address: this.userDetail.address,
      role: this.userDetail.role,
      isVerified: this.userDetail.isVerified
    };
    this.showEditForm = true;
  }

  saveEdit(): void {
    if (!this.userDetail || !this.editForm) return;
    this.isSavingEdit = true;
    this.editError = '';

    this.userService.updateUser(this.userDetail.id, this.editForm)
      .pipe(finalize(() => this.isSavingEdit = false))
      .subscribe({
        next: (res) => {
          if (res.data) {
            this.userDetail = { ...this.userDetail, ...res.data };
            this.editSuccess = '✓ User updated.';
            this.showEditForm = false;
            
            const idx = this.allUsers.findIndex(u => u.id === res.data!.id);
            if (idx !== -1) this.allUsers[idx] = res.data!;
            
            this.applyFilters();
          }
          this.cdr.detectChanges();
          setTimeout(() => this.editSuccess = '', 3000);
        },
        error: (err) => {
          this.editError = err.error?.message ?? 'Failed to update user.';
          this.cdr.detectChanges();
        }
      });
  }

  toggleVerified(): void {
    if (!this.userDetail) return;
    const newValue = !this.userDetail.isVerified;

    this.userService.updateUser(this.userDetail.id, { isVerified: newValue }).subscribe({
      next: (res) => {
        if (res.data) {
          this.userDetail!.isVerified = res.data.isVerified;
          this.actionSuccess = res.data.isVerified ? '✓ User verified.' : '✓ User unverified.';
          
          const idx = this.allUsers.findIndex(u => u.id === res.data!.id);
          if (idx !== -1) this.allUsers[idx].isVerified = res.data.isVerified;
          
          this.applyFilters();
        }
        this.cdr.detectChanges();
        setTimeout(() => this.actionSuccess = '', 3000);
      },
      error: (err) => {
        this.actionError = err.error?.message ?? 'Failed to update verification.';
        this.cdr.detectChanges();
      }
    });
  }

  deleteUser(): void {
    if (!this.userDetail) return;
    this.isDeletingUser = true;

    this.userService.deleteUser(this.userDetail.id, this.deletionNote).subscribe({
      next: (res) => {
        if (res.success) {
          this.actionSuccess = 'User deleted successfully.';
          this.showDeleteConfirm = false;
          this.showUserModal = false;
          
          const idx = this.allUsers.findIndex(u => u.id === this.userDetail!.id);
          if (idx !== -1) this.allUsers[idx].isDeleted = true;
          
          this.applyFilters();
        }
        this.isDeletingUser = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.actionError = err.error?.message ?? 'Failed to delete user.';
        this.isDeletingUser = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ─── UI HELPERS ────────────────────────────────────────────────────────────

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }

  getScoreClass(score: number): string {
    if (score >= 70) return 'text-emerald-400';
    if (score >= 40) return 'text-amber-400';
    return 'text-red-400';
  }

  getFineStatusClass(status: string): string {
    switch (status) {
      case 'Unpaid': return 'text-red-400';
      case 'PendingVerification': return 'text-amber-400';
      case 'Paid': return 'text-emerald-400';
      default: return 'text-zinc-400';
    }
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned': return 'bg-cyan-600 text-white border-zinc-400/20';
      case 'late': return 'bg-red-500/10 text-red-400 border-red-500/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good': return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair': return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor': return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}