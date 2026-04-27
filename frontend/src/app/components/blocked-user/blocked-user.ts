import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { UserBlockService } from '../../services/userBlockService';
import { UserBlockListDto } from '../../dtos/userBlockDto';
import { UserBlockFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

@Component({
  selector: 'app-blocked-user',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './blocked-user.html',
  styleUrl: './blocked-user.css',
})
export class BlockedUser implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  isLoading = true;
  listLoading = false;
  listError: string | null = null;

  blockedUsers: UserBlockListDto[] = [];
  totalCount = 0;
  currentPage = 1;
  readonly pageSize = 10;

  searchQuery = '';
  sortFilter = 'newest';

  userToUnblock: UserBlockListDto | null = null;
  isUnblocking = false;

  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  constructor(
    private userBlockService: UserBlockService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadBlockedUsers();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadBlockedUsers();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    clearTimeout(this.toastTimeout);
  }

  loadBlockedUsers(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const filter: UserBlockFilter = { search: this.searchQuery.trim() || null };
    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    this.userBlockService
      .getMyBlocks(filter, request)
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
            this.blockedUsers = res.data.items;
            this.totalCount = res.data.totalCount;
          } else {
            this.listError = res.message || 'Kunne ikke hente blokerede brugere.';
          }
        },
        error: () => {
          this.listError = 'Der opstod en fejl. Prøv igen.';
        },
      });
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  onFilterChange(): void { this.currentPage = 1; this.loadBlockedUsers(); }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadBlockedUsers();
  }

  trackById(_: number, user: UserBlockListDto): string { return user.blockedId; }

  openUnblockConfirm(user: UserBlockListDto): void { this.userToUnblock = user; }

  confirmUnblock(): void {
    if (!this.userToUnblock) return;
    const targetUser = this.userToUnblock;
    this.isUnblocking = true;

    this.userBlockService
      .unblockUser(targetUser.blockedId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isUnblocking = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.userToUnblock = null;
            this.totalCount = Math.max(0, this.totalCount - 1);
            if (this.blockedUsers.length === 1 && this.currentPage > 1) {
              this.currentPage--;
            }
            this.loadBlockedUsers();
            this.showToast(`${targetUser.blockedName} er ikke længere blokeret.`);
          }
        },
        error: (err) => {
          this.userToUnblock = null;
          this.showToast(err.error?.message ?? 'Kunne ikke fjerne blokering.');
        },
      });
  }

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.markForCheck();
    clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.markForCheck();
    }, 3000);
  }

  getInitials(name: string): string {
    return (name || '?')
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }
}
