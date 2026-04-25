import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
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

  // Unblock confirm modal
  userToUnblock: UserBlockListDto | null = null;
  isUnblocking = false;

  // Toast
  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  constructor(
    private userBlockService: UserBlockService,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadBlockedUsers();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadBlockedUsers();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    clearTimeout(this.toastTimeout);
  }

  // ─── Load ─────────────────────────────────────────────────────────────────────

  loadBlockedUsers(): void {
    if (this.isLoading) {
      this.listLoading = true;
    }
    this.listError = null;

    const filter: UserBlockFilter = {
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: this.sortFilter !== 'oldest',
    };

    console.log('filter:', JSON.stringify(filter));
  console.log('request:', JSON.stringify(request));

    this.userBlockService.getMyBlocks(filter, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.listLoading = false;
          this.isLoading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (res) => {
            console.log('blocks response:', JSON.stringify(res));
          if (res.success && res.data) {
            this.blockedUsers = res.data.items;
            console.log('Blocked users loaded:', this.blockedUsers);
            this.totalCount = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load blocked users.';
          }
        },
        error: () => {
          this.listError = 'An error occurred. Please try again.';
        },
      });
  }

  // ─── Search & filters ─────────────────────────────────────────────────────────

  onSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadBlockedUsers();
  }

  // ─── Pagination ───────────────────────────────────────────────────────────────

  get totalPages(): number {
    return getTotalPages(this.totalCount, this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadBlockedUsers();
  }

  trackById(_: number, user: UserBlockListDto): string {
    return user.blockedId;
  }

  // ─── Unblock ──────────────────────────────────────────────────────────────────

  openUnblockConfirm(user: UserBlockListDto): void {
    this.userToUnblock = user;
  }

  confirmUnblock(): void {
    if (!this.userToUnblock) return;
    const targetUser = this.userToUnblock;
    this.isUnblocking = true;

    this.userBlockService.unblockUser(targetUser.blockedId)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isUnblocking = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.userToUnblock = null;
            this.totalCount = Math.max(0, this.totalCount - 1);
            // If page is now empty and not on first page, go back one
            if (this.blockedUsers.length === 1 && this.currentPage > 1) {
              this.currentPage--;
            }
            this.loadBlockedUsers();
            this.showToast(`${targetUser.blockedName} has been unblocked.`);
          }
        },
        error: (err) => {
          this.userToUnblock = null;
          this.showToast(err.error?.message ?? 'Failed to unblock user.');
        },
      });
  }

  // ─── UI helpers ───────────────────────────────────────────────────────────────

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
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
}