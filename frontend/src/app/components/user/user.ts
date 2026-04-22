import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { Navbar } from "../navbar/navbar";
import { UserPublicProfileDto } from '../../dtos/userDto';
import { ItemListDto } from '../../dtos/itemDto';
import { UserReviewListDto } from '../../dtos/userReviewDto';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { ItemService } from '../../services/itemService';
import { UserReviewService } from '../../services/userReviewService';
import { ItemAvailability, ItemCondition, ReportReason, ReportType } from '../../dtos/enums';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';
import { ReportService } from '../../services/reportService';
import { ItemFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';

@Component({
  selector: 'app-user',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserProfile implements OnInit, OnDestroy {

  userId = '';
  currentUserId = '';
  profile: UserPublicProfileDto | null = null;
  filteredItems: ItemListDto[] = [];
  reviews: UserReviewListDto[] = [];
  ItemAvailability = ItemAvailability;
  itemsCollapsed = false;

  isLoadingProfile = true;
  isLoadingItems = true;
  isLoadingReviews = true;
  isInitialItemLoad = true;

  // Item filters
  searchQuery = '';
  availableOnly = false;
  freeOnly = false;
  sortLabel = 'newest';
  sortBy = 'createdAt';
  sortDescending = true;

  itemTotalCount = 0;
  //Items pagination
  itemPage = 1;
  profileTotalItems = 0;

  //pfp modal
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
  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10;
  }

  get scoreColor(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'score--green';
    if (s >= 40) return 'score--amber';
    return 'score--red';
  }

  get scoreBg(): string {
    const s = this.profile?.score ?? 0;
    if (s >= 70) return 'score-chip--green';
    if (s >= 40) return 'score-chip--amber';
    return 'score-chip--red';
  }

  // ── trackBy helpers ───────────────────────────────────────────────────────
  trackByIndex(_index: number, value: number): number { return value; }
  trackByItemId(_index: number, item: ItemListDto): string { return item.slug; }
  trackByReviewId(_index: number, review: UserReviewListDto): string {
    return review.reviewerId + review.createdAt;
  }

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private itemService: ItemService,
    private reviewService: UserReviewService,
    private reportService: ReportService,
    private route: ActivatedRoute,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    // Debounce search input — fires 350ms after user stops typing
    this.searchSubject.pipe(
      debounceTime(350),
      takeUntil(this.destroy$)
    ).subscribe(() => {
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
    this.route.params.subscribe(params => {
      this.userId = params['id'];

      const qp = this.route.snapshot.queryParams;
      this.itemPage = parseInt(qp['page']) || 1;

      this.load();
    });
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  private load(): void {
    this.isLoadingProfile = true;
    this.isLoadingReviews = true;
    this.isInitialItemLoad = true;
    this.profile = null;
    this.filteredItems = [];
    this.reviews = [];
    this.reviewPage = 1;
    this.itemTotalCount = 0;

    this.userService.getMyProfile().subscribe({
      next: (res) => {
        this.currentUserId = res.data?.id ?? '';
        this.cdr.detectChanges();
      },
      error: () => { }
    });

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

    this.loadInitialTotal();
    this.loadItems();

    this.reviewService.getReviewsForUser(this.userId, {}, { page: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.reviews = res.data?.items ?? [];
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingReviews = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadInitialTotal(): void {
    this.itemService.getPublicByOwner(this.userId, {}, { page: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        this.profileTotalItems = res.data?.totalCount ?? 0;
        this.cdr.detectChanges();
      }
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
      }
    });
  }

  onSearch(): void {
    this.cdr.detectChanges();
    this.searchSubject.next(this.searchQuery);
  }

  onAvailableToggle(): void {
    this.availableOnly = !this.availableOnly;
    this.itemPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

  onSortChange(value: string): void {
    this.sortLabel = value;
    switch (value) {
      case 'newest':     this.sortBy = 'createdAt';     this.sortDescending = true;  break;
      case 'oldest':     this.sortBy = 'createdAt';     this.sortDescending = false; break;
      case 'rating':     this.sortBy = 'averageRating'; this.sortDescending = true;  break;
      case 'az':         this.sortBy = 'title';         this.sortDescending = false; break;
      case 'za':         this.sortBy = 'title';         this.sortDescending = true;  break;
      case 'price_asc':  this.sortBy = 'pricePerDay';   this.sortDescending = false; break;
      case 'price_desc': this.sortBy = 'pricePerDay';   this.sortDescending = true;  break;
      default:           this.sortBy = 'createdAt';     this.sortDescending = true;
    }
    this.itemPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

  onFreeToggle(): void {
    this.freeOnly = !this.freeOnly;
    this.itemPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

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

    this.reportService.create({
      type: ReportType.User,
      targetId: this.userId,
      reasons: this.reportReason as ReportReason,
      additionalDetails: this.reportDetails.trim() || null,
    }).subscribe({
      next: () => {
        this.isSubmittingReport = false;
        this.reportSuccess = 'Report submitted. Thank you.';
        this.cdr.detectChanges();
        setTimeout(() => { this.showReportModal = false; }, 1500);
      },
      error: (err) => {
        this.reportError = err.error?.message ?? 'Failed to submit report.';
        this.isSubmittingReport = false;
        this.cdr.detectChanges();
      }
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

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getCategoryEmoji(name: string): string { return getCategoryEmoji(name); }
  getConditionClass(condition: ItemCondition): string { return getConditionClass(condition); }
  getAvailabilityClass(availability: ItemAvailability): string { return getAvailabilityClass(availability); }
  getAvailabilityLabel(availability: ItemAvailability): string { return getAvailabilityLabel(availability); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}