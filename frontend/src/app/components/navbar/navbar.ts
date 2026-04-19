import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
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


export class Navbar implements OnInit {

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

    private hub: NotificationHubService | null = null;


  constructor(
    private authService: AuthService,
    public router: Router,
    private userService: UserService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.isHomePage = this.router.url.startsWith('/home');

    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: NavigationEnd) => {
      this.isHomePage = e.urlAfterRedirects.startsWith('/home');
      this.cdr.detectChanges();
    });

    if(this.authService.isLoggedIn()) {
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


  private connectNotificationHubService(): void {
    const token = this.authService.getToken();
    if (!token) return;
 
    this.hub = new NotificationHubService(token);
 
    this.hub.onReceiveNotification((notification: NotificationDto) => {
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
 
    this.hub.onUnreadCountUpdated((count: number) => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    });
 
    this.hub.start().catch(err =>
      console.warn('Notification hub failed to start:', err)
    );
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
    if (this.hub) {
      this.hub.off();
      this.hub.stop();
    }
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
      //Loan lifecycle
      case NotificationType.LoanRequested: return '📋';
      case NotificationType.LoanApproved: return '✅';
      case NotificationType.LoanRejected: return '❌';
      case NotificationType.LoanCancelled: return '🚫';
      case NotificationType.LoanActive: return '🤝';
      case NotificationType.LoanReturned: return '📦';
      //Due dates
      case NotificationType.DueSoon: return '⏰';
      case NotificationType.LoanOverdue: return '🔴';
      //Item lifecycle
      case NotificationType.ItemApproved: return '✅';
      case NotificationType.ItemRejected: return '❌';
      case NotificationType.ItemAvailable: return '🟢';
      //Fines and score
      case NotificationType.FineIssued: return '⚠️';
      case NotificationType.FinePaid: return '💰';
      case NotificationType.ScoreChanged: return '📊';
      //Disputes
      case NotificationType.DisputeFiled: return '⚖️';
      case NotificationType.DisputeResponseSubmitted: return '📝';
      case NotificationType.DisputeResolved: return '🏛️';
      //Appeals
      case NotificationType.AppealSubmitted: return '📤';
      case NotificationType.AppealApproved: return '✅';
      case NotificationType.AppealRejected: return '❌';
      //Verification
      case NotificationType.VerificationApproved: return '🏅';
      case NotificationType.VerificationRejected: return '❌';
      //Messages
      case NotificationType.LoanMessageReceived: return '💬';
      case NotificationType.DirectMessageReceived: return '✉️';
      case NotificationType.SupportMessageReceived: return '🎧';
      default: return '🔔';
    }
  }
 
  getNotificationIconBg(type: NotificationType): string {
    switch (type) {
      //Green — positive outcomes
      case NotificationType.LoanApproved:
      case NotificationType.LoanActive:
      case NotificationType.ItemApproved:
      case NotificationType.ItemAvailable:
      case NotificationType.FinePaid:
      case NotificationType.AppealApproved:
      case NotificationType.VerificationApproved:
        return 'bg-emerald-400/10';
      //Red — negative outcomes
      case NotificationType.LoanRejected:
      case NotificationType.LoanCancelled:
      case NotificationType.ItemRejected:
      case NotificationType.FineIssued:
      case NotificationType.LoanOverdue:
      case NotificationType.AppealRejected:
      case NotificationType.VerificationRejected:
        return 'bg-red-400/10';
      //Amber — pending / action needed
      case NotificationType.LoanRequested:
      case NotificationType.DueSoon:
      case NotificationType.DisputeFiled:
      case NotificationType.DisputeResponseSubmitted:
      case NotificationType.AppealSubmitted:
        return 'bg-amber-400/10';
      //Blue — informational
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
