import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Navbar } from "../navbar/navbar";
import { UserListForUsersDto, UserPublicProfileDto } from '../../dtos/userDto';
import { ItemListDto } from '../../dtos/itemDto';
import { UserReviewDto, UserReviewListDto } from '../../dtos/userReviewDto';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { ItemService } from '../../services/itemService';
import { UserReviewService } from '../../services/userReviewService';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';

@Component({
  selector: 'app-user',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserProfile implements OnInit {

  userId = '';
  profile: UserPublicProfileDto | null = null;
  items: ItemListDto[] = [];
  filteredItems: ItemListDto[] = [];
  reviews: UserReviewListDto[] = [];
  displayedReviews: UserReviewListDto[] = [];
  ItemAvailability = ItemAvailability;
  itemsCollapsed = false;

  isLoadingProfile = true;
  isLoadingItems = true;
  isLoadingReviews = true;

  //Item filters
  searchQuery = '';
  activeFilter: 'all' | 'available' | 'onloan' = 'all';
  visibleReviews = 5;

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10;
  }

  get scoreColor(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'text-emerald-400';
    if (s >= 40) return 'text-amber-400';
    return 'text-red-400';
  }

  get scoreBg(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'bg-emerald-400/10 border-emerald-400/20';
    if (s >= 40) return 'bg-amber-400/10 border-amber-400/20';
    return 'bg-red-400/10 border-red-400/20';
  }

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private itemService: ItemService,
    private reviewService: UserReviewService,
    private route: ActivatedRoute,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.params.subscribe(params => {
      this.userId = params['id'];
      this.load();
    });
  }

  private load(): void {
    this.isLoadingProfile = true;
    this.isLoadingItems = true;
    this.isLoadingReviews = true;
    this.profile = null;
    this.items = [];
    this.reviews = [];
    this.visibleReviews = 5;

    this.userService.getPublicProfile(this.userId).subscribe({
      next: (res) => {
        this.profile = res.data;
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      }
    });

  this.itemService.getPublicByOwner(
    this.userId,
    {},
    { page: 1, pageSize: 50 }
  ).subscribe({
    next: (res) => {
      this.items = res.data?.items ?? [];
      this.applyFilters();
      this.isLoadingItems = false;
      this.cdr.detectChanges();
    },
    error: () => {
      this.isLoadingItems = false;
      this.cdr.detectChanges();
    }
  });

    this.reviewService.getReviewsForUser(
      this.userId,
      {},
      { page: 1, pageSize: 50 }
    ).subscribe({
      next: (res) => {
        this.reviews = res.data?.items ?? [];
        this.updateDisplayedReviews();
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    let result = [...this.items];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.title.toLowerCase().includes(q) ||
        i.categoryName.toLowerCase().includes(q)
      );
    }

    switch (this.activeFilter) {
      case 'available': result = result.filter(i => i.isActive && i.availability === ItemAvailability.Available); break;
      case 'onloan':    result = result.filter(i => i.availability === ItemAvailability.OnRent); break;
    }

    this.filteredItems = result;
    this.cdr.detectChanges();
  }

  updateDisplayedReviews(): void {
    this.displayedReviews = this.reviews.slice(0, this.visibleReviews);
  }

  loadMoreReviews(): void {
    this.visibleReviews += 5;
    this.updateDisplayedReviews();
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat?.toLowerCase()] ?? '📦';
  }

  getConditionClass(condition: ItemCondition): string {
    switch (condition) {
      case ItemCondition.Excellent: return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case ItemCondition.Good: return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case ItemCondition.Fair: return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case ItemCondition.Poor: return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default: return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }


}