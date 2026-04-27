import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { NotificationDto } from '../../dtos/notificationDto';
import { NotificationReferenceType, NotificationType } from '../../dtos/enums';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { ItemService } from '../../services/itemService';
import { NotificationHubService } from '../../services/notificationHubService';
import { NotificationService } from '../../services/notificationService';
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements OnInit, OnDestroy {
  userName = '';
  userEmail = '';
  userInitials = '';
  userAvatarUrl: string | null = null;

  showUserMenu = false;
  showNotifications = false;

  notifications: NotificationDto[] = [];
  unreadCount = 0;

  searchQuery = '';
  isHomePage = false;
  isAdmin = false;
  currentUserId = '';

  showBanToast = false;
  navigatingIds = new Set<number>();

  readonly theme = inject(ThemeService);
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    public router: Router,
    private userService: UserService,
    private itemService: ItemService,
    private notificationService: NotificationService,
    private notificationHubService: NotificationHubService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.isHomePage = this.router.url.startsWith('/home');

    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e: NavigationEnd) => {
        this.isHomePage = e.urlAfterRedirects.startsWith('/home');
        this.cdr.detectChanges();
      });

    if (this.authService.isLoggedIn()) {
      this.loadUserInfo();
      this.loadSummary();
      this.connectHub();
    }
  }

  ngOnDestroy(): void {
    this.notificationHubService.off();
    this.notificationHubService.stop();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Notification hub ─────────────────────────────────────────────────────
  private connectHub(): void {
    this.notificationHubService.onReceiveNotification((n: NotificationDto) => {
      const exists = this.notifications.some((x) => x.id === n.id);
      if (!exists) {
        this.notifications.unshift({ ...n, isRead: false });
        this.unreadCount++;
        this.cdr.detectChanges();
      }

      const isSystemAlert =
        n.type === NotificationType.SystemAlert ||
        (n.referenceType as any) === NotificationReferenceType.SystemAlert ||
        (n.referenceType as any) === 'SystemAlert';

      if (isSystemAlert) {
        this.showBanToast = true;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.showBanToast = false;
          this.logout();
        }, 5000);
      }
    });

    this.notificationHubService.onUnreadCountUpdated((count: number) => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    });

    this.notificationHubService.onNewMessageNotification(() => {
      this.unreadCount++;
      this.cdr.detectChanges();
    });

    this.notificationHubService
      .start()
      .catch((err) => console.warn('Notification hub failed to start:', err));
  }

  private loadUserInfo(): void {
    this.userService.getMyProfile().subscribe({
      next: (res) => {
        const user = res.data!;
        this.userName = user.fullName || user.username;
        this.userEmail = user.email;
        this.userInitials = this.getInitials(this.userName);
        this.userAvatarUrl = user.avatarUrl ?? null;
        this.currentUserId = user.id;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load user info:', err),
    });
  }

  loadSummary(): void {
    this.notificationService.getSummary().subscribe({
      next: (res) => {
        const data = res.data;
        if (!data) return;
        this.unreadCount = data.unreadCount;
        this.notifications = data.recent;
        this.cdr.detectChanges();
      },
      error: () => {},
    });
  }

  // ─── Notification click flow ──────────────────────────────────────────────
  onNotificationClick(n: NotificationDto): void {
    if (!n.isRead) {
      n.isRead = true;
      this.unreadCount = Math.max(0, this.unreadCount - 1);
      this.cdr.detectChanges();
      this.notificationService
        .markAsRead(n.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          error: () => {
            n.isRead = false;
            this.unreadCount++;
            this.cdr.detectChanges();
          },
        });
    }

    this.showNotifications = false;

    if (n.referenceType === NotificationReferenceType.SystemAlert) {
      this.userService.getMyProfile().subscribe({
        next: (res) => {
          if (res.data?.isBanned) {
            this.triggerBanToast();
          }
        },
        error: (err) => {
          if (err.status === 403 || err.status === 401) {
            this.triggerBanToast();
          }
        },
      });
      return;
    }

    this.navigate(n);
  }

  private triggerBanToast(): void {
    this.showBanToast = true;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.showBanToast = false;
      this.logout();
    }, 5000);
  }

  private navigate(n: NotificationDto): void {
    if (!n.referenceId || !n.referenceType) return;
    if (this.navigatingIds.has(n.id)) return;

    const type = n.referenceType;
    const id = n.referenceId;

    if (type === NotificationReferenceType.Item) {
      if (n.type === NotificationType.ItemDeleted) {
        this.router.navigate(['/my-items']);
        return;
      }
      this.navigatingIds.add(n.id);
      this.cdr.detectChanges();
      this.itemService
        .getById(id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.navigatingIds.delete(n.id);
            const slug = res.data?.slug;
            if (slug) this.router.navigate(['/items', slug]);
            else this.router.navigate(['/my-items']);
            this.cdr.detectChanges();
          },
          error: () => {
            this.navigatingIds.delete(n.id);
            this.router.navigate(['/my-items']);
            this.cdr.detectChanges();
          },
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

  // ─── Other actions ───────────────────────────────────────────────────────
  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach((n) => (n.isRead = true));
      this.unreadCount = 0;
      this.cdr.detectChanges();
    });
  }

  onSearch(): void {
    this.router.navigate(['/home'], {
      queryParams: { q: this.searchQuery.trim() || null },
      queryParamsHandling: 'merge',
    });
  }

  logout(): void {
    this.notificationHubService.off();
    this.notificationHubService.stop();
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.authService.clearTokens();
        this.router.navigate(['/']);
      },
    });
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  // ─── Notification visuals ─────────────────────────────────────────────────
  getNotificationIcon(type: NotificationType): string {
    switch (type) {
      case NotificationType.LoanRequested: return '📩';
      case NotificationType.LoanApproved: return '✅';
      case NotificationType.LoanRejected: return '❌';
      case NotificationType.LoanCancelled: return '🚫';
      case NotificationType.LoanActive: return '🤝';
      case NotificationType.LoanReturned: return '📦';
      case NotificationType.DueSoon: return '⏰';
      case NotificationType.LoanOverdue: return '🚨';
      case NotificationType.ItemApproved: return '✨';
      case NotificationType.ItemPendingReview: return '🔍';
      case NotificationType.ItemRejected: return '🛠️';
      case NotificationType.ItemAvailable: return '🟢';
      case NotificationType.ItemDeleted: return '🗑️';
      case NotificationType.FineIssued: return '⚠️';
      case NotificationType.FinePaymentPendingVerification: return '📑';
      case NotificationType.FinePaid: return '💰';
      case NotificationType.FineRejected: return '👎';
      case NotificationType.FineVoided: return '🛡️';
      case NotificationType.ScoreChanged: return '📈';
      case NotificationType.DisputeFiled: return '⚖️';
      case NotificationType.DisputeResponseSubmitted: return '📝';
      case NotificationType.DisputeResolved: return '🏛️';
      case NotificationType.DisputeExpired: return '⌛';
      case NotificationType.AppealSubmitted: return '📤';
      case NotificationType.AppealApproved: return '🔓';
      case NotificationType.AppealRejected: return '🔒';
      case NotificationType.VerificationSubmitted: return '🆔';
      case NotificationType.VerificationApproved: return '🏅';
      case NotificationType.VerificationRejected: return '✖️';
      case NotificationType.LoanMessageReceived: return '💬';
      case NotificationType.DirectMessageReceived: return '✉️';
      case NotificationType.SupportMessageReceived: return '🎧';
      case NotificationType.SupportThreadCreated: return '🎫';
      case NotificationType.SupportThreadClaimed: return '🙋';
      case NotificationType.SupportThreadClosed: return '📁';
      case NotificationType.ReportSubmitted: return '🚩';
      case NotificationType.ReportResolved: return '✅';
      default: return '🔔';
    }
  }

  /**
   * Returns a BEM modifier class for the icon tile, keyed on category.
   * The monochrome design renders all variants neutrally — they remain
   * distinct in markup so future themes can colour-differentiate them.
   */
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
        return 'notif-icon--success';
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
        return 'notif-icon--danger';
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
        return 'notif-icon--warning';
      case NotificationType.ScoreChanged:
      case NotificationType.DisputeResolved:
      case NotificationType.LoanMessageReceived:
      case NotificationType.DirectMessageReceived:
      case NotificationType.SupportMessageReceived:
      case NotificationType.SupportThreadClaimed:
      case NotificationType.SupportThreadClosed:
        return 'notif-icon--info';
      default:
        return 'notif-icon--neutral';
    }
  }
}
