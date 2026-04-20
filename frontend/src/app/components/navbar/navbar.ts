import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NotificationDto } from '../../dtos/notificationDTO';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { NotificationHubService } from '../../services/notificationHubService';
import { NotificationService } from '../../services/notificationService';
import { NotificationType } from '../../dtos/enums';
import { ThemeService } from '../../services/themeService';

/** A single item in the user dropdown menu. */
interface MenuItem {
  label: string;
  /** Static route — preferred when no dynamic values are needed. */
  link?: string;
  /** Use when the route depends on component state (e.g. currentUserId). */
  action?: () => void;
  /** Only shown to admins. */
  adminOnly?: boolean;
}

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements OnInit, OnDestroy {

  // ─── User info ───────────────────────────────────────────────────────
  userName = '';
  userEmail = '';
  userInitials = '';
  userAvatarUrl: string | null = null;
  currentUserId = '';
  isAdmin = false;

  // ─── UI state ────────────────────────────────────────────────────────
  showUserMenu = false;
  showNotifications = false;

  // ─── Notifications ───────────────────────────────────────────────────
  notifications: NotificationDto[] = [];
  unreadCount = 0;

  private hub: NotificationHubService | null = null;

  // ─── Theme (public so the template can bind to it) ────────────────────
  readonly theme = inject(ThemeService);

  constructor(
    private authService: AuthService,
    public router: Router,
    private userService: UserService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();

    if (this.authService.isLoggedIn()) {
      this.loadUserInfo();
      this.loadSummary();
      this.connectNotificationHubService();
    }
  }

  ngOnDestroy(): void {
    if (this.hub) {
      this.hub.off();
      this.hub.stop();
    }
  }

  // ─── Menu config (template iterates over this) ────────────────────────
  /**
   * The user dropdown menu. Items with `action` run a callback; items with
   * `link` use plain routerLink. Keeping this as data avoids 6+ near-
   * identical <a> blocks in the template.
   */
  readonly menuItems: MenuItem[] = [
    { label: 'Min Profil',       action: () => this.router.navigate(['/users', this.currentUserId]) },
    { label: 'Mit Dashboard',    link: '/my-dashboard' },
    { label: 'Mine Annoncer',    link: '/my-items' },
    { label: 'Mine Lejer',       link: '/my-loans' },
    { label: 'Favoritter',       link: '/favorites' },
    { label: 'Løsningscenter',   link: '/my-resolution-center' },
    { label: 'Admin Dashboard',  link: '/admin-dashboard', adminOnly: true },
  ];

  // ─── Event handlers ──────────────────────────────────────────────────
  onMenuItemClick(item: MenuItem): void {
    this.showUserMenu = false;
    if (item.action) item.action();
  }

  onNotificationClick(n: NotificationDto): void {
    if (n.isRead) return;

    n.isRead = true;
    this.unreadCount = Math.max(0, this.unreadCount - 1);

    this.notificationService.markAsRead(n.id).subscribe({
      error: () => {
        n.isRead = false;
        this.unreadCount++;
        this.cdr.detectChanges();
      },
    });
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => (n.isRead = true));
      this.unreadCount = 0;
      this.cdr.detectChanges();
    });
  }

  logout(): void {
    if (this.hub) {
      this.hub.off();
      this.hub.stop();
    }
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.authService.clearTokens();
        this.router.navigate(['/']);
      },
    });
  }

  // ─── Loaders / hub ───────────────────────────────────────────────────
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

  private loadSummary(): void {
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

  private connectNotificationHubService(): void {
    const token = this.authService.getToken();
    if (!token) return;

    this.hub = new NotificationHubService(token);

    this.hub.onReceiveNotification((notification: NotificationDto) => {
      const exists = this.notifications.some(n => n.id === notification.id);
      if (exists) return;

      this.notifications.unshift({
        id: notification.id,
        message: notification.message,
        type: notification.type,
        referenceId: notification.referenceId,
        referenceType: notification.referenceType,
        isRead: false,
        createdAt: notification.createdAt,
      });
      this.unreadCount++;
      this.cdr.detectChanges();
    });

    this.hub.onUnreadCountUpdated((count: number) => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    });

    this.hub.start().catch(err =>
      console.warn('Notification hub failed to start:', err),
    );
  }

  // ─── Helpers ─────────────────────────────────────────────────────────
  private getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getNotificationIcon(type: NotificationType): string {
    const ICON_MAP: Partial<Record<NotificationType, string>> = {
      [NotificationType.LoanRequested]:             '📋',
      [NotificationType.LoanApproved]:              '✅',
      [NotificationType.LoanRejected]:              '❌',
      [NotificationType.LoanCancelled]:             '🚫',
      [NotificationType.LoanActive]:                '🤝',
      [NotificationType.LoanReturned]:              '📦',
      [NotificationType.DueSoon]:                   '⏰',
      [NotificationType.LoanOverdue]:               '🔴',
      [NotificationType.ItemApproved]:              '✅',
      [NotificationType.ItemRejected]:              '❌',
      [NotificationType.ItemAvailable]:             '🟢',
      [NotificationType.FineIssued]:                '⚠️',
      [NotificationType.FinePaid]:                  '💰',
      [NotificationType.ScoreChanged]:              '📊',
      [NotificationType.DisputeFiled]:              '⚖️',
      [NotificationType.DisputeResponseSubmitted]:  '📝',
      [NotificationType.DisputeResolved]:           '🏛️',
      [NotificationType.AppealSubmitted]:           '📤',
      [NotificationType.AppealApproved]:            '✅',
      [NotificationType.AppealRejected]:            '❌',
      [NotificationType.VerificationApproved]:      '🏅',
      [NotificationType.VerificationRejected]:      '❌',
      [NotificationType.LoanMessageReceived]:       '💬',
      [NotificationType.DirectMessageReceived]:     '✉️',
      [NotificationType.SupportMessageReceived]:    '🎧',
    };
    return ICON_MAP[type] ?? '🔔';
  }
}
