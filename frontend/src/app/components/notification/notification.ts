import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { NotificationService } from '../../services/notificationService';
import { ItemService } from '../../services/itemService';
import { NotificationDto } from '../../dtos/notificationDto';
import { NotificationReferenceType, NotificationType } from '../../dtos/enums';
import { getPageNumbers } from '../../utils/pagination.utils';
import { NotificationFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';

type TabId = 'all' | 'unread';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  count?: number;
}

@Component({
  selector: 'app-notification',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './notification.html',
  styleUrl: './notification.css',
})
export class Notification implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();

  isLoading = true;
  error: string | null = null;

  notifications: NotificationDto[] = [];
  totalCount = 0;

  // Sort & filter
  sortBy = 'createdAt';
  sortDescending = true;
  typeFilter: NotificationType | null = null;

  readonly pageSize = 15;

  activeTab: TabId = 'all';
  tabs: Tab[] = [
    { id: 'all',    label: 'All',    icon: '▤' },
    { id: 'unread', label: 'Unread', icon: '●' },
  ];

  currentPage = 1;

  deletingIds = new Set<number>();
  showDeleteAllConfirm = false;
  isDeletingAll = false;
  navigatingIds = new Set<number>();

  // Expose NotificationType to template
  NotificationType = NotificationType;

  constructor(
    private notificationService: NotificationService,
    private itemService: ItemService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  // ─── Computed ─────────────────────────────────────────────────────────────────

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  get hasUnread(): boolean {
    return this.notifications.some(n => !n.isRead);
  }

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadNotifications();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────────

  loadNotifications(): void {
    this.isLoading = true;
    this.error = null;

    const filter: NotificationFilter = {
      isRead: this.activeTab === 'unread' ? false : null,
      type: this.typeFilter,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
    };

    this.notificationService.getAll(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.notifications = res.data.items;
            this.totalCount = res.data.totalCount;
            this.refreshTabCounts();
          } else {
            this.error = res.message || 'Failed to load notifications.';
          }
        },
        error: () => { this.error = 'An error occurred. Please try again.'; },
      });
  }

  private refreshTabCounts(): void {
    const allTab    = this.tabs.find(t => t.id === 'all');
    const unreadTab = this.tabs.find(t => t.id === 'unread');

    // Update the active tab count from the current response
    if (this.activeTab === 'all' && allTab) {
      allTab.count = this.totalCount;
    } else if (this.activeTab === 'unread' && unreadTab) {
      unreadTab.count = this.totalCount;
    }

    // Always fetch the unread count for the badge
    this.notificationService.getAll({ isRead: false }, { page: 1, pageSize: 1 })
      .pipe(takeUntil(this.destroy$))
      .subscribe(res => {
        if (unreadTab && res.data) {
          unreadTab.count = res.data.totalCount;
          this.cdr.markForCheck();
        }
      });

    // If we're on unread tab, also refresh the all count
    if (this.activeTab === 'unread') {
      this.notificationService.getAll({}, { page: 1, pageSize: 1 })
        .pipe(takeUntil(this.destroy$))
        .subscribe(res => {
          if (allTab && res.data) {
            allTab.count = res.data.totalCount;
            this.cdr.markForCheck();
          }
        });
    }
  }

  // ─── Tab & filter controls ────────────────────────────────────────────────────

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadNotifications();
  }

  onSortChange(sortBy: string): void {
    if (this.sortBy === sortBy) {
      this.sortDescending = !this.sortDescending;
    } else {
      this.sortBy = sortBy;
      this.sortDescending = true;
    }
    this.currentPage = 1;
    this.loadNotifications();
  }

  onTypeFilterChange(value: string): void {
    this.typeFilter = value === '' ? null : +value as unknown as NotificationType;
    this.currentPage = 1;
    this.loadNotifications();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadNotifications();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // ─── Click handler ────────────────────────────────────────────────────────────

  onNotificationClick(n: NotificationDto): void {
    if (!n.isRead) {
      n.isRead = true;
      this.cdr.markForCheck();
      this.notificationService.markAsRead(n.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => this.refreshTabCounts(),
          error: () => {
            n.isRead = false;
            this.cdr.markForCheck();
          }
        });
    }
    this.navigate(n);
  }

  // ─── Navigation ───────────────────────────────────────────────────────────────

  navigate(n: NotificationDto): void {
    if (!n.referenceId || !n.referenceType) return;
    if (this.navigatingIds.has(n.id)) return;

    const type = n.referenceType;
    const id   = n.referenceId;

    if (type === NotificationReferenceType.Item) {
      if (n.type === NotificationType.ItemDeleted) {
        this.router.navigate(['/my-items']);
        return;
      }
      this.navigatingIds.add(n.id);
      this.cdr.markForCheck();
      this.itemService.getById(id).subscribe({
        next: (res) => {
          this.navigatingIds.delete(n.id);
          const slug = res.data?.slug;
          if (slug) this.router.navigate(['/items', slug]);
          else      this.router.navigate(['/my-items']);
          this.cdr.markForCheck();
        },
        error: () => {
          this.navigatingIds.delete(n.id);
          this.router.navigate(['/my-items']);
          this.cdr.markForCheck();
        }
      });
      return;
    }

    if (type === NotificationReferenceType.Loan) {
      this.router.navigate(['/loans', id]);
      return;
    }

    if (type === NotificationReferenceType.Fine) {
      this.router.navigate(['/resolution-center'], { queryParams: { tab: 'fines', fineId: id } });
      return;
    }

    if (type === NotificationReferenceType.Dispute) {
      this.router.navigate(['/resolution-center'], { queryParams: { tab: 'disputes', disputeId: id } });
      return;
    }

    if (type === NotificationReferenceType.Appeal) {
      this.router.navigate(['/resolution-center'], { queryParams: { tab: 'appeals', appealId: id } });
      return;
    }

    if (type === NotificationReferenceType.Verification) {
      this.router.navigate(['/resolution-center'], { queryParams: { tab: 'verification', verificationId: id } });
      return;
    }

    if (type === NotificationReferenceType.Report) {
      this.router.navigate(['/resolution-center'], { queryParams: { tab: 'reports', reportId: id } });
      return;
    }

    if (type === NotificationReferenceType.DirectConversation) {
      this.router.navigate(['/my-chats'], { queryParams: { conversationId: id } });
      return;
    }
  }

  isNavigable(n: NotificationDto): boolean {
    if (!n.referenceId || !n.referenceType) return false;
    const noNav: NotificationReferenceType[] = [
      NotificationReferenceType.DirectConversation,
      NotificationReferenceType.SupportThread,
    ];
    return !noNav.includes(n.referenceType);
  }

  // ─── Bulk actions ─────────────────────────────────────────────────────────────

  markAllAsRead(): void {
    this.notificationService.markAllAsRead()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.notifications.forEach(n => n.isRead = true);
          this.refreshTabCounts();
          this.cdr.markForCheck();
          // If on unread tab, reload to clear the list
          if (this.activeTab === 'unread') {
            this.currentPage = 1;
            this.loadNotifications();
          }
        }
      });
  }

  deleteNotification(id: number): void {
    this.deletingIds.add(id);
    this.cdr.markForCheck();
    this.notificationService.delete(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.deletingIds.delete(id);
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications = this.notifications.filter(n => n.id !== id);
          this.totalCount = Math.max(0, this.totalCount - 1);
          // If page is now empty and not the first page, go back one
          if (this.notifications.length === 0 && this.currentPage > 1) {
            this.currentPage--;
            this.loadNotifications();
          } else {
            this.refreshTabCounts();
          }
          this.cdr.markForCheck();
        }
      });
  }

  deleteAll(): void {
    this.isDeletingAll = true;
    this.notificationService.deleteAll()
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.isDeletingAll = false;
        this.showDeleteAllConfirm = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications = [];
          this.totalCount = 0;
          this.currentPage = 1;
          this.tabs.forEach(t => t.count = 0);
          this.cdr.markForCheck();
        }
      });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────────

  trackById(_: number, n: NotificationDto): number { return n.id; }

  getNotificationIcon(type: NotificationType): string {
    switch (type) {
      case NotificationType.LoanRequested:                  return '📩';
      case NotificationType.LoanApproved:                   return '✅';
      case NotificationType.LoanRejected:                   return '❌';
      case NotificationType.LoanCancelled:                  return '🚫';
      case NotificationType.LoanActive:                     return '🤝';
      case NotificationType.LoanReturned:                   return '📦';
      case NotificationType.DueSoon:                        return '⏳';
      case NotificationType.LoanOverdue:                    return '🚨';
      case NotificationType.ItemApproved:                   return '✨';
      case NotificationType.ItemPendingReview:              return '🔍';
      case NotificationType.ItemRejected:                   return '🛠️';
      case NotificationType.ItemAvailable:                  return '🟢';
      case NotificationType.ItemDeleted:                    return '🗑️';
      case NotificationType.FineIssued:                     return '⚠️';
      case NotificationType.FinePaymentPendingVerification: return '📑';
      case NotificationType.FinePaid:                       return '💰';
      case NotificationType.FineRejected:                   return '👎';
      case NotificationType.FineVoided:                     return '🛡️';
      case NotificationType.ScoreChanged:                   return '📈';
      case NotificationType.DisputeFiled:                   return '⚖️';
      case NotificationType.DisputeResponseSubmitted:       return '📝';
      case NotificationType.DisputeResolved:                return '🏛️';
      case NotificationType.DisputeExpired:                 return '⌛';
      case NotificationType.AppealSubmitted:                return '📤';
      case NotificationType.AppealApproved:                 return '🔓';
      case NotificationType.AppealRejected:                 return '🔒';
      case NotificationType.VerificationSubmitted:          return '🆔';
      case NotificationType.VerificationApproved:           return '🏅';
      case NotificationType.VerificationRejected:           return '✖️';
      case NotificationType.LoanMessageReceived:            return '💬';
      case NotificationType.DirectMessageReceived:          return '✉️';
      case NotificationType.SupportMessageReceived:         return '🎧';
      case NotificationType.SupportThreadCreated:           return '🎫';
      case NotificationType.SupportThreadClaimed:           return '🙋';
      case NotificationType.SupportThreadClosed:            return '📁';
      case NotificationType.ReportSubmitted:                return '🚩';
      case NotificationType.ReportResolved:                 return '✅';
      default:                                              return '🔔';
    }
  }

  getNotificationIconBg(type: NotificationType): string {
    switch (type) {
      case NotificationType.LoanApproved:
      case NotificationType.LoanActive:
      case NotificationType.LoanReturned:
      case NotificationType.ItemApproved:
      case NotificationType.ItemAvailable:
      case NotificationType.FinePaid:
      case NotificationType.FineVoided:
      case NotificationType.AppealApproved:
      case NotificationType.VerificationApproved:
      case NotificationType.ReportResolved:
        return 'bg-emerald-400/10';

      case NotificationType.LoanRejected:
      case NotificationType.LoanCancelled:
      case NotificationType.LoanOverdue:
      case NotificationType.ItemRejected:
      case NotificationType.ItemDeleted:
      case NotificationType.FineIssued:
      case NotificationType.FineRejected:
      case NotificationType.AppealRejected:
      case NotificationType.VerificationRejected:
      case NotificationType.DisputeExpired:
        return 'bg-red-400/10';

      case NotificationType.LoanRequested:
      case NotificationType.DueSoon:
      case NotificationType.ItemPendingReview:
      case NotificationType.FinePaymentPendingVerification:
      case NotificationType.DisputeFiled:
      case NotificationType.DisputeResponseSubmitted:
      case NotificationType.AppealSubmitted:
      case NotificationType.VerificationSubmitted:
      case NotificationType.ReportSubmitted:
      case NotificationType.SupportThreadCreated:
        return 'bg-amber-400/10';

      case NotificationType.ScoreChanged:
      case NotificationType.DisputeResolved:
      case NotificationType.LoanMessageReceived:
      case NotificationType.DirectMessageReceived:
      case NotificationType.SupportMessageReceived:
      case NotificationType.SupportThreadClaimed:
      case NotificationType.SupportThreadClosed:
        return 'bg-blue-400/10';

      default:
        return 'bg-zinc-800';
    }
  }

  getTypeBadgeClass(type: NotificationType): string {
    const bg = this.getNotificationIconBg(type);
    if (bg.includes('emerald')) return 'bg-emerald-400/10 text-emerald-400';
    if (bg.includes('red'))     return 'bg-red-400/10 text-red-400';
    if (bg.includes('amber'))   return 'bg-amber-400/10 text-amber-400';
    if (bg.includes('blue'))    return 'bg-blue-400/10 text-blue-400';
    return 'bg-zinc-800 text-zinc-400';
  }

  getTypeLabel(type: NotificationType): string {
    const loanTypes = [
      NotificationType.LoanRequested, NotificationType.LoanApproved,
      NotificationType.LoanRejected,  NotificationType.LoanCancelled,
      NotificationType.LoanActive,    NotificationType.DueSoon,
      NotificationType.LoanOverdue,   NotificationType.LoanMessageReceived,
      NotificationType.LoanReturned,
    ];
    const itemTypes = [
      NotificationType.ItemApproved, NotificationType.ItemPendingReview,
      NotificationType.ItemRejected, NotificationType.ItemAvailable,
      NotificationType.ItemDeleted,
    ];
    const fineTypes = [
      NotificationType.FineIssued, NotificationType.FinePaymentPendingVerification,
      NotificationType.FinePaid,   NotificationType.FineRejected,
      NotificationType.FineVoided,
    ];
    const disputeTypes = [
      NotificationType.DisputeFiled,    NotificationType.DisputeResponseSubmitted,
      NotificationType.DisputeResolved, NotificationType.DisputeExpired,
    ];
    const appealTypes = [
      NotificationType.AppealSubmitted, NotificationType.AppealApproved,
      NotificationType.AppealRejected,
    ];
    const verificationTypes = [
      NotificationType.VerificationApproved, NotificationType.VerificationRejected,
      NotificationType.VerificationSubmitted,
    ];
    const supportTypes = [
      NotificationType.SupportMessageReceived, NotificationType.SupportThreadCreated,
      NotificationType.SupportThreadClosed,    NotificationType.SupportThreadClaimed,
    ];
    const reportTypes = [NotificationType.ReportSubmitted, NotificationType.ReportResolved];

    if (loanTypes.includes(type))         return 'Loan';
    if (itemTypes.includes(type))         return 'Item';
    if (fineTypes.includes(type))         return 'Fine';
    if (disputeTypes.includes(type))      return 'Dispute';
    if (appealTypes.includes(type))       return 'Appeal';
    if (verificationTypes.includes(type)) return 'Verification';
    if (supportTypes.includes(type))      return 'Support';
    if (reportTypes.includes(type))       return 'Report';
    if (type === NotificationType.DirectMessageReceived) return 'Message';
    if (type === NotificationType.ScoreChanged)          return 'Score';
    return 'General';
  }
}