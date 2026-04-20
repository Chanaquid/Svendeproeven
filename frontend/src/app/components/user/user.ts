import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
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

@Component({
  selector: 'app-user',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './user.html',
  styleUrl: './user.css',
})
export class UserProfile implements OnInit {

  userId = '';
  currentUserId = '';
  profile: UserPublicProfileDto | null = null;
  items: ItemListDto[] = [];
  filteredItems: ItemListDto[] = [];
  reviews: UserReviewListDto[] = [];
  ItemAvailability = ItemAvailability;
  itemsCollapsed = false;

  isLoadingProfile = true;
  isLoadingItems = true;
  isLoadingReviews = true;

  // Item filters
  searchQuery = '';
  activeFilter: 'all' | 'available' = 'all';

  // Items pagination
  itemPage = 1;
  itemPageSize = 20;



  get itemTotalPages(): number {
    return getTotalPages(this.filteredItems.length, this.itemPageSize);
  }
  get itemPageNumbers(): number[] {
    return getPageNumbers(this.itemPage, this.itemTotalPages);
  }
  get pagedItems(): ItemListDto[] {
    const start = (this.itemPage - 1) * this.itemPageSize;
    return this.filteredItems.slice(start, start + this.itemPageSize);
  }
  goToItemPage(page: number): void {
    if (page < 1 || page > this.itemTotalPages) return;
    this.itemPage = page;
    this.cdr.detectChanges();
  }

  // Reviews pagination
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

  // Report modal
  showReportModal = false;
  reportReason: ReportReason | '' = '';
  reportDetails = '';
  isSubmittingReport = false;
  reportSuccess = '';
  reportError = '';

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
    this.itemPage = 1;
    this.reviewPage = 1;

    this.userService.getMyProfile().subscribe({
      next: (res) => { this.currentUserId = res.data?.id ?? ''; this.cdr.detectChanges(); },
      error: () => { }
    });

    this.userService.getPublicProfile(this.userId).subscribe({
      next: (res) => { this.profile = res.data; this.isLoadingProfile = false; this.cdr.detectChanges(); },
      error: () => { this.isLoadingProfile = false; this.cdr.detectChanges(); }
    });

    this.itemService.getPublicByOwner(this.userId, {}, { page: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.items = res.data?.items ?? [];
        this.applyFilters();
        this.isLoadingItems = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoadingItems = false; this.cdr.detectChanges(); }
    });

    this.reviewService.getReviewsForUser(this.userId, {}, { page: 1, pageSize: 200 }).subscribe({
      next: (res) => { this.reviews = res.data?.items ?? []; this.isLoadingReviews = false; this.cdr.detectChanges(); },
      error: () => { this.isLoadingReviews = false; this.cdr.detectChanges(); }
    });
  }

  applyFilters(): void {
    let result = [...this.items];
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i => i.title.toLowerCase().includes(q) || i.categoryName.toLowerCase().includes(q));
    }
    if (this.activeFilter === 'available') {
      result = result.filter(i => i.isActive && i.availability === ItemAvailability.Available);
    }
    this.filteredItems = result;
    this.itemPage = 1;
    this.cdr.detectChanges();
  }

  openReportModal(): void {
    this.reportReason = '';
    this.reportDetails = '';
    this.reportSuccess = '';
    this.reportError = '';
    this.showReportModal = true;
  }

  submitReport(): void {
    if (!this.reportReason) { this.reportError = 'Please select a reason.'; return; }
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

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getCategoryEmoji(name: string): string { return getCategoryEmoji(name); }
  getConditionClass(condition: ItemCondition): string { return getConditionClass(condition); }
  getAvailabilityClass(availability: ItemAvailability): string { return getAvailabilityClass(availability); }
  getAvailabilityLabel(availability: ItemAvailability): string { return getAvailabilityLabel(availability); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}