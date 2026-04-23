<<<<<<< Updated upstream
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Navbar } from '../navbar/navbar';
import { UserListForUsersDto, UserPublicProfileDto } from '../../dtos/userDTO';
import { ItemListDto } from '../../dtos/itemDTO';
import { UserReviewDto, UserReviewListDto } from '../../dtos/userReviewDto';
=======
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { Navbar } from '../navbar/navbar';
import { UserPublicProfileDto } from '../../dtos/userDto';
import { ItemListDto } from '../../dtos/itemDto';
import { UserReviewListDto } from '../../dtos/userReviewDto';
>>>>>>> Stashed changes
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { ItemService } from '../../services/itemService';
import { UserReviewService } from '../../services/userReviewService';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';

@Component({
  selector: 'app-user',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
<<<<<<< Updated upstream
export class UserProfile implements OnInit {
=======
export class User implements OnInit, OnDestroy {
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
=======
  itemTotalCount = 0;
  // Items pagination
  itemPage = 1;
  profileTotalItems = 0;

  // PFP modal
  showPfpModal = false;

  get itemPageSize(): number {
    const cardMinWidth = 240;
    const gap = 16;
    const pagePadding = 80;
    const availableWidth = window.innerWidth - pagePadding;
    const cols = Math.max(1, Math.floor((availableWidth + gap) / (cardMinWidth + gap)));
    return cols * 3;
  }

  get itemTotalPages(): number {
    return getTotalPages(this.itemTotalCount, this.itemPageSize);
  }

  get itemPageNumbers(): number[] {
    return getPageNumbers(this.itemPage, this.itemTotalPages);
  }

  goToItemPage(page: number): void {
    if (page < 1 || page > this.itemTotalPages) return;
    this.itemPage = page;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page },
      queryParamsHandling: 'merge',
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadItems();
  }

  // ── Reviews pagination (client-side, independent) ────────────────────────
  reviewPage = 1;
  reviewPageSize = 5;

  get reviewTotalPages(): number {
    return getTotalPages(this.reviews.length, this.reviewPageSize);
  }

  get reviewPageNumbers(): number[] {
    return getPageNumbers(this.reviewPage, this.reviewTotalPages);
  }

  get displayedReviews(): UserReviewListDto[] {
    const start = (this.reviewPage - 1) * this.reviewPageSize;
    return this.reviews.slice(start, start + this.reviewPageSize);
  }

  goToReviewPage(page: number): void {
    if (page < 1 || page > this.reviewTotalPages) return;
    this.reviewPage = page;
    this.cdr.detectChanges();
  }

  // ── Report modal ──────────────────────────────────────────────────────────
  showReportModal = false;
  reportReason: ReportReason | '' = '';
  reportDetails = '';
  isSubmittingReport = false;
  reportSuccess = '';
  reportError = '';

  // ── RxJS subjects ─────────────────────────────────────────────────────────
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // ── Resize listener ref ───────────────────────────────────────────────────
  private resizeHandler = () => {
    this.itemPage = 1;
    this.loadItems();
  };

  // ── Computed ──────────────────────────────────────────────────────────────
>>>>>>> Stashed changes
  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return (
      Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10
    );
  }

  /** Plain CSS class for score tier — styled in user.css */
  get scoreColor(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'score-text--good';
    if (s >= 40) return 'score-text--medium';
    return 'score-text--low';
  }

  /** Plain CSS class for score chip background — styled in user.css */
  get scoreBg(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'score-chip--good';
    if (s >= 40) return 'score-chip--medium';
    return 'score-chip--low';
  }

<<<<<<< Updated upstream
  private emojiMap: Record<string, string> = {
    electronics: '📱',
    tools: '🔧',
    sports: '⚽',
    music: '🎸',
    books: '📚',
    camping: '⛺',
    photography: '📷',
    gaming: '🎮',
    gardening: '🌱',
    biking: '🚲',
    kitchen: '🍳',
    cleaning: '🧹',
    fashion: '👗',
    art: '🎨',
    baby: '👶',
    events: '🎉',
    auto: '🚗',
    other: '📦',
  };
=======
  // ── trackBy helpers ───────────────────────────────────────────────────────
  trackByIndex(_index: number, value: number): number {
    return value;
  }
  trackByItemId(_index: number, item: ItemListDto): string {
    return item.slug;
  }
  trackByReviewId(_index: number, review: UserReviewListDto): string {
    return review.reviewerId + review.createdAt;
  }
>>>>>>> Stashed changes

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

<<<<<<< Updated upstream
=======
    // Debounce search input — fires 350ms after user stops typing
    this.searchSubject.pipe(debounceTime(350), takeUntil(this.destroy$)).subscribe(() => {
      this.itemPage = 1;
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: 1 },
        queryParamsHandling: 'merge',
      });
      this.loadItems();
    });

    window.addEventListener('resize', this.resizeHandler);

    // Read userId from route params, then read page from query params
>>>>>>> Stashed changes
    this.route.params.subscribe((params) => {
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
<<<<<<< Updated upstream
    this.visibleReviews = 5;
=======
    this.reviewPage = 1;
    this.itemTotalCount = 0;

    this.userService.getMyProfile().subscribe({
      next: (res) => {
        this.currentUserId = res.data?.id ?? '';
        this.cdr.detectChanges();
      },
      error: () => {},
    });
>>>>>>> Stashed changes

    this.userService.getPublicProfile(this.userId).subscribe({
      next: (res) => {
        this.profile = res.data;
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
    });

    this.itemService.getPublicByOwner(this.userId, {}, { page: 1, pageSize: 50 }).subscribe({
      next: (res) => {
        this.items = res.data?.items ?? [];
        this.applyFilters();
        this.isLoadingItems = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingItems = false;
        this.cdr.detectChanges();
      },
    });

    this.reviewService.getReviewsForUser(this.userId, {}, { page: 1, pageSize: 50 }).subscribe({
      next: (res) => {
        this.reviews = res.data?.items ?? [];
        this.updateDisplayedReviews();
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
<<<<<<< Updated upstream
      },
=======
      },
    });
  }

  private loadInitialTotal(): void {
    this.itemService.getPublicByOwner(this.userId, {}, { page: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        this.profileTotalItems = res.data?.totalCount ?? 0;
        this.cdr.detectChanges();
      },
    });
  }

  /**
   * Fetches items from backend. Shows skeleton only on first load —
   * subsequent calls (search, filter, sort, page change) swap content silently.
   */
  private loadItems(): void {
    if (this.isInitialItemLoad) {
      this.isLoadingItems = true;
      this.cdr.detectChanges();
    }

    const pageSize = Math.max(this.itemPageSize, 1);

    const filter: ItemFilter = {};
    if (this.searchQuery.trim()) filter.search = this.searchQuery.trim();
    if (this.availableOnly) filter.availability = ItemAvailability.Available;

    if (this.freeOnly) {
      filter.isFree = true;
    } else if (this.sortBy === 'pricePerDay') {
      // When sorting by price, explicitly exclude free items
      filter.isFree = false;
    }

    const paging: PagedRequest = {
      page: this.itemPage,
      pageSize,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
    };

    this.itemService.getPublicByOwner(this.userId, filter, paging).subscribe({
      next: (res) => {
        this.filteredItems = res.data?.items ?? [];
        this.itemTotalCount = res.data?.totalCount ?? 0;
        this.isLoadingItems = false;
        this.isInitialItemLoad = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingItems = false;
        this.isInitialItemLoad = false;
        this.cdr.detectChanges();
      },
>>>>>>> Stashed changes
    });
  }

  applyFilters(): void {
    let result = [...this.items];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(
        (i) => i.title.toLowerCase().includes(q) || i.categoryName.toLowerCase().includes(q),
      );
    }

    switch (this.activeFilter) {
      case 'available':
        result = result.filter((i) => i.isActive && i.availability === ItemAvailability.Available);
        break;
      case 'onloan':
        result = result.filter((i) => i.availability === ItemAvailability.OnRent);
        break;
    }

    this.filteredItems = result;
    this.cdr.detectChanges();
  }

  updateDisplayedReviews(): void {
    this.displayedReviews = this.reviews.slice(0, this.visibleReviews);
  }

<<<<<<< Updated upstream
  loadMoreReviews(): void {
    this.visibleReviews += 5;
    this.updateDisplayedReviews();
=======
  onSortChange(value: string): void {
    this.sortLabel = value;
    switch (value) {
      case 'newest':
        this.sortBy = 'createdAt';
        this.sortDescending = true;
        break;
      case 'oldest':
        this.sortBy = 'createdAt';
        this.sortDescending = false;
        break;
      case 'rating':
        this.sortBy = 'averageRating';
        this.sortDescending = true;
        break;
      case 'az':
        this.sortBy = 'title';
        this.sortDescending = false;
        break;
      case 'za':
        this.sortBy = 'title';
        this.sortDescending = true;
        break;
      case 'price_asc':
        this.sortBy = 'pricePerDay';
        this.sortDescending = false;
        break;
      case 'price_desc':
        this.sortBy = 'pricePerDay';
        this.sortDescending = true;
        break;
      default:
        this.sortBy = 'createdAt';
        this.sortDescending = true;
    }
    this.itemPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
>>>>>>> Stashed changes
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat?.toLowerCase()] ?? '📦';
  }

<<<<<<< Updated upstream
  /** Plain CSS class for item condition — styled in user.css */
  getConditionClass(condition: ItemCondition): string {
    switch (condition) {
      case ItemCondition.Excellent:
        return 'condition-badge--excellent';
      case ItemCondition.Good:
        return 'condition-badge--good';
      case ItemCondition.Fair:
        return 'condition-badge--fair';
      case ItemCondition.Poor:
        return 'condition-badge--poor';
      default:
        return 'condition-badge--default';
    }
  }

  getInitials(name: string): string {
    return (
      name
        ?.split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
        .slice(0, 2) ?? ''
    );
  }
=======
  openReportModal(): void {
    this.reportReason = '';
    this.reportDetails = '';
    this.reportSuccess = '';
    this.reportError = '';
    this.showReportModal = true;
  }

  submitReport(): void {
    if (!this.reportReason) {
      this.reportError = 'Please select a reason.';
      return;
    }
    this.isSubmittingReport = true;
    this.reportError = '';

    this.reportService
      .create({
        type: ReportType.User,
        targetId: this.userId,
        reasons: this.reportReason as ReportReason,
        additionalDetails: this.reportDetails.trim() || null,
      })
      .subscribe({
        next: () => {
          this.isSubmittingReport = false;
          this.reportSuccess = 'Report submitted. Thank you.';
          this.cdr.detectChanges();
          setTimeout(() => {
            this.showReportModal = false;
          }, 1500);
        },
        error: (err) => {
          this.reportError = err.error?.message ?? 'Failed to submit report.';
          this.isSubmittingReport = false;
          this.cdr.detectChanges();
        },
      });
  }

  togglePfpModal(): void {
    // Only allow expanding if there is an actual image
    if (this.profile?.avatarUrl) {
      this.showPfpModal = !this.showPfpModal;

      // Prevent scrolling when modal is open
      if (this.showPfpModal) {
        document.body.style.overflow = 'hidden';
      } else {
        document.body.style.overflow = 'auto';
      }
    }
  }

  goToItem(slug: string): void {
    this.router.navigate(['/items', slug]);
  }

  getCategoryEmoji(name: string): string {
    return getCategoryEmoji(name);
  }
  getConditionClass(condition: ItemCondition): string {
    return getConditionClass(condition);
  }
  getAvailabilityClass(availability: ItemAvailability): string {
    return getAvailabilityClass(availability);
  }
  getAvailabilityLabel(availability: ItemAvailability): string {
    return getAvailabilityLabel(availability);
  }
  getInitials(name: string): string {
    return (
      name
        ?.split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
        .slice(0, 2) ?? ''
    );
  }
>>>>>>> Stashed changes
}
