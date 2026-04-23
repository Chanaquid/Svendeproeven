import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NotificationDto } from '../../dtos/notificationDto';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { NotificationHubService } from '../../services/notificationHubService';
import { NotificationService } from '../../services/notificationService';
import { NotificationType } from '../../dtos/enums';

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

  constructor(
    private authService: AuthService,
    public router: Router,
    private userService: UserService,
    private notificationService: NotificationService,
    private notificationHubService: NotificationHubService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.isHomePage = this.router.url.startsWith('/home');

    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: NavigationEnd) => {
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
  }

  private connectHub(): void {
    this.notificationHubService.onReceiveNotification((notification: NotificationDto) => {
      const exists = this.notifications.some(n => n.id === notification.id);
      if (!exists) {
        this.notifications.unshift({
          id: notification.id,
          message: notification.message,
          type: notification.type,
          referenceId: notification.referenceId,
          referenceType: notification.referenceType,
          isRead: false,
          createdAt: notification.createdAt
        });
        this.unreadCount++;
        this.cdr.detectChanges();
      }
    });

    this.notificationHubService.onUnreadCountUpdated((count: number) => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    });

    this.notificationHubService.onNewMessageNotification((data) => {
      // Real-time red dot bump when a message arrives on another page
      this.unreadCount++;
      this.cdr.detectChanges();
    });

    this.notificationHubService.start().catch(err =>
      console.warn('Notification hub failed to start:', err)
    );
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
      error: (err) => console.error('Failed to load user info:', err)
    });
  }

  loadSummary(): void {
    this.notificationService.getSummary().subscribe({
      next: (res) => {
        const data = res.data;
        if (!data) return;
        this.unreadCount = data.unreadCount;
        this.notifications = data.recent;
        console.log(this.notifications);
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  onNotificationClick(n: NotificationDto): void {
    if (!n.isRead) {
      n.isRead = true;
      this.unreadCount = Math.max(0, this.unreadCount - 1);
      this.notificationService.markAsRead(n.id).subscribe({
        error: () => {
          n.isRead = false;
          this.unreadCount++;
          this.cdr.detectChanges();
        }
      });
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => n.isRead = true);
      this.unreadCount = 0;
      this.cdr.detectChanges();
    });
  }

  onSearch(): void {
    this.router.navigate(['/home'], {
      queryParams: { q: this.searchQuery.trim() || null },
      queryParamsHandling: 'merge'
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
      }
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getNotificationIcon(type: NotificationType): string {
    switch (type) {
      case NotificationType.LoanRequested: return '📋';
      case NotificationType.LoanApproved: return '✅';
      case NotificationType.LoanRejected: return '❌';
      case NotificationType.LoanCancelled: return '🚫';
      case NotificationType.LoanActive: return '🤝';
      case NotificationType.LoanReturned: return '📦';
      case NotificationType.DueSoon: return '⏰';
      case NotificationType.LoanOverdue: return '🔴';
      case NotificationType.ItemApproved: return '✅';
      case NotificationType.ItemRejected: return '❌';
      case NotificationType.ItemAvailable: return '🟢';
      case NotificationType.FineIssued: return '⚠️';
      case NotificationType.FinePaid: return '💰';
      case NotificationType.ScoreChanged: return '📊';
      case NotificationType.DisputeFiled: return '⚖️';
      case NotificationType.DisputeResponseSubmitted: return '📝';
      case NotificationType.DisputeResolved: return '🏛️';
      case NotificationType.AppealSubmitted: return '📤';
      case NotificationType.AppealApproved: return '✅';
      case NotificationType.AppealRejected: return '❌';
      case NotificationType.VerificationApproved: return '🏅';
      case NotificationType.VerificationRejected: return '❌';
      case NotificationType.LoanMessageReceived: return '💬';
      case NotificationType.DirectMessageReceived: return '✉️';
      case NotificationType.SupportMessageReceived: return '🎧';
      default: return '🔔';
    }
  }

  getNotificationIconBg(type: NotificationType): string {
    switch (type) {
      case NotificationType.LoanApproved:
      case NotificationType.LoanActive:
      case NotificationType.ItemApproved:
      case NotificationType.ItemAvailable:
      case NotificationType.FinePaid:
      case NotificationType.AppealApproved:
      case NotificationType.VerificationApproved:
        return 'bg-emerald-400/10';
      case NotificationType.LoanRejected:
      case NotificationType.LoanCancelled:
      case NotificationType.ItemRejected:
      case NotificationType.FineIssued:
      case NotificationType.LoanOverdue:
      case NotificationType.AppealRejected:
      case NotificationType.VerificationRejected:
        return 'bg-red-400/10';
      case NotificationType.LoanRequested:
      case NotificationType.DueSoon:
      case NotificationType.DisputeFiled:
      case NotificationType.DisputeResponseSubmitted:
      case NotificationType.AppealSubmitted:
        return 'bg-amber-400/10';
      case NotificationType.LoanReturned:
      case NotificationType.ScoreChanged:
      case NotificationType.DisputeResolved:
      case NotificationType.LoanMessageReceived:
      case NotificationType.DirectMessageReceived:
      case NotificationType.SupportMessageReceived:
        return 'bg-blue-400/10';
      default:
        return 'bg-zinc-800';
    }
  }
}