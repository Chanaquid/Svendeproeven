import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { UserService } from '../../services/userService';
import { LoanService } from '../../services/loanService';
import { ItemAvailability } from '../../dtos/enums';
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit {
  readonly theme = inject(ThemeService);

  featuredItems: any[] = [];
  stats = {
    activeItems: '...',
    members: '...',
    completedLoans: '...',
  };

  steps = [
    { number: '1', icon: '📝', title: 'Opslå' },
    { number: '2', icon: '📅', title: 'Book' },
    { number: '3', icon: '🤝', title: 'Afhent' },
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    private itemService: ItemService,
    private userService: UserService,
    private loanService: LoanService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadStats();
  }

  private loadStats() {
    this.itemService.getAvailableCount().subscribe({
      next: (res) => {
        this.stats.activeItems = res.data + '+';
        this.cdr.detectChanges();
      },
      error: () => {
        this.stats.activeItems = '250+';
        this.cdr.detectChanges();
      },
    });

    this.loanService.getCompletedLoansCount().subscribe({
      next: (count) => {
        this.stats.completedLoans = count + '+';
        this.cdr.detectChanges();
      },
      error: () => {
        this.stats.completedLoans = '50+';
        this.cdr.detectChanges();
      },
    });

    this.itemService.getLatest(4).subscribe({
      next: (res) => {
        const items = res.data ?? [];
        this.featuredItems = items.map((item) => ({
          id: item.id,
          emoji: this.getCategoryEmoji(item.categoryName),
          category: item.categoryName,
          title: item.title,
          location: item.pickupAddress,
          owner: item.ownerName,
          ownerInitials: (item.ownerName || '')
            .split(' ')
            .map((n: string) => n[0])
            .join('')
            .toUpperCase()
            .slice(0, 2),
          rating:
            item.averageRating && item.averageRating > 0
              ? `${item.averageRating.toFixed(1)} (${item.totalReviews})`
              : 'Ny',
          available: item.availability === ItemAvailability.Available,
          primaryPhotoUrl: item.mainPhotoUrl,
        }));
        this.cdr.detectChanges();
      },
      error: () => this.cdr.detectChanges(),
    });

    this.userService.getTotalUsersCount().subscribe({
      next: (users) => {
        this.stats.members = users.data + '+';
        this.cdr.detectChanges();
      },
      error: () => {
        this.stats.members = '5000+';
        this.cdr.detectChanges();
      },
    });
  }

  private getCategoryEmoji(categoryName: string): string {
    const map: Record<string, string> = {
      tools: '🔧', photography: '📷', bikes: '🚲', outdoors: '🏕️',
      music: '🎸', gaming: '🎮', books: '📚', travel: '🧳',
      garden: '🪴', art: '🎨', fashion: '👗', kitchen: '🍳',
      electronics: '📱', sports: '⚽', camping: '🏕️', others: '📦',
    };
    return map[categoryName?.toLowerCase()] ?? '📦';
  }
}
