import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { Navbar } from '../navbar/navbar';
import { ItemListDto } from '../../dtos/itemDto';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { UserFavoriteService } from '../../services/userFavoriteService';
import { ItemService } from '../../services/itemService';
import { ItemFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('categoryStrip') categoryStrip!: ElementRef;
  ItemAvailability = ItemAvailability;

  userName = '';
  isLoading = true;
  isInitialLoad = true;
  items: ItemListDto[] = [];

  // Search and filters
  searchQuery = '';
  selectedCategory: string | null = null;
  sortLabel = 'newest';
  sortBy = 'createdAt';
  sortDescending = true;
  availableOnly = false;
  freeOnly = false;

  showLeftArrow = false;
  showRightArrow = false;

  favoriteIds = new Set<number>();
  togglingIds = new Set<number>();

  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  currentPage = 1;
  totalCount = 0;
  currentUserId = '';

  // ── Dynamic page size — cols × 3 rows, matches CSS minmax(240px, 1fr) ────
  get pageSize(): number {
    const cardMinWidth = 240;
    const gap = 16;
    const pagePadding = 80; // page body padding both sides
    const availableWidth = window.innerWidth - pagePadding;
    const cols = Math.max(1, Math.floor((availableWidth + gap) / (cardMinWidth + gap)));
    return cols * 3;
  }

  categories = [
    { icon: '📱', name: 'Electronics' },
    { icon: '🔧', name: 'Tools' },
    { icon: '⚽', name: 'Sports' },
    { icon: '🎸', name: 'Music' },
    { icon: '📚', name: 'Books' },
    { icon: '⛺', name: 'Camping' },
    { icon: '📷', name: 'Photography' },
    { icon: '🎮', name: 'Gaming' },
    { icon: '🌱', name: 'Gardening' },
    { icon: '🚲', name: 'Biking' },
    { icon: '🍳', name: 'Kitchen' },
    { icon: '🧹', name: 'Cleaning' },
    { icon: '👗', name: 'Fashion' },
    { icon: '🎨', name: 'Art' },
    { icon: '👶', name: 'Baby' },
    { icon: '🎉', name: 'Events' },
    { icon: '🚗', name: 'Auto' },
    { icon: '📦', name: 'Other' },
  ];

  private isDragging = false;
  private dragStartX = 0;
  private scrollStartX = 0;

  private categoryIdMap: Record<string, number> = {
    'Electronics': 1, 'Tools': 2, 'Sports': 3, 'Music': 4,
    'Books': 5, 'Camping': 6, 'Photography': 7, 'Gaming': 8,
    'Gardening': 9, 'Biking': 10, 'Kitchen': 11, 'Cleaning': 12,
    'Fashion': 13, 'Art': 14, 'Baby': 15, 'Events': 16,
    'Auto': 17, 'Other': 18,
  };

  // ── RxJS ──────────────────────────────────────────────────────────────────
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // ── Resize listener ───────────────────────────────────────────────────────
  private resizeHandler = () => {
    this.currentPage = 1;
    this.loadItems();
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private userService: UserService,
    private itemService: ItemService,
    private favoriteService: UserFavoriteService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.loadUserInfo();
    this.loadFavorites();

    // Debounce search — 350ms after user stops typing
    this.searchSubject.pipe(
      debounceTime(350),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: 1 },
        queryParamsHandling: 'merge',
      });
      this.loadItems();
    });

    window.addEventListener('resize', this.resizeHandler);

    // Read page + search query from URL so back-navigation restores state
    this.route.queryParams.subscribe(params => {
      const q = params['q'] || '';
      const page = parseInt(params['page']) || 1;
      this.searchQuery = q;
      this.currentPage = page;
      this.loadItems();
    });
  }

  ngAfterViewInit(): void {
    const el = this.categoryStrip.nativeElement;

    const updateArrows = () => {
      this.showLeftArrow = el.scrollLeft > 0;
      this.showRightArrow = el.scrollLeft + el.clientWidth < el.scrollWidth - 1;
      this.cdr.detectChanges();
    };

    setTimeout(() => updateArrows(), 100);
    el.addEventListener('scroll', updateArrows);
    window.addEventListener('resize', updateArrows);

    let isMouseDown = false;
    let startMouseX = 0;
    let startScrollLeft = 0;
    let hasDragged = false;

    el.addEventListener('mousedown', (event: MouseEvent) => {
      isMouseDown = true;
      hasDragged = false;
      startMouseX = event.pageX;
      startScrollLeft = el.scrollLeft;
      el.style.userSelect = 'none';
    });

    window.addEventListener('mouseup', () => {
      isMouseDown = false;
      el.style.cursor = 'grab';
      el.style.userSelect = '';
    });

    window.addEventListener('mousemove', (e: MouseEvent) => {
      if (!isMouseDown) return;
      const movedDistance = e.pageX - startMouseX;
      if (Math.abs(movedDistance) > 5) {
        hasDragged = true;
        el.style.cursor = 'grabbing';
        el.scrollLeft = startScrollLeft - movedDistance;
      }
    });

    el.addEventListener('click', (e: MouseEvent) => {
      if (hasDragged) {
        e.stopPropagation();
        hasDragged = false;
      }
    }, true);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadUserInfo(): void {
    this.userService.getMyProfile().subscribe({
      next: (user) => {
        this.userName = user.data?.fullName ?? '';
        this.currentUserId = user.data?.id ?? '';
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load user info:', err)
    });
  }

  /**
   * Fetches items from backend using dynamic pageSize (cols × 3 rows).
   * Shows skeleton only on first load — subsequent fetches swap content silently.
   */
  loadItems(): void {
    if (this.isInitialLoad) {
      this.isLoading = true;
      this.cdr.detectChanges();
    }

    const filter: ItemFilter = {
      ...(this.searchQuery.trim() && { search: this.searchQuery.trim() }),
      ...(this.selectedCategory && { categoryId: this.categoryIdMap[this.selectedCategory] }),
      ...(this.availableOnly && { availability: ItemAvailability.Available }),
      ...(this.freeOnly ? { isFree: true } : (this.sortBy === 'pricePerDay' && { isFree: false })),
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: Math.max(this.pageSize, 1),
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
    };

    this.itemService.getAllApproved(filter, request).subscribe({
      next: (res) => {
        this.items = res.data?.items ?? [];
        this.totalCount = res.data?.totalCount ?? 0;
        this.isLoading = false;
        this.isInitialLoad = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load items:', err);
        this.isLoading = false;
        this.isInitialLoad = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadFavorites(): void {
    const request: PagedRequest = { page: 1, pageSize: 100 };
    this.favoriteService.getMyFavorites(request).subscribe({
      next: (favs) => {
        this.favoriteIds = new Set((favs.data?.items ?? []).map(i => i.id));
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load favorites:', err)
    });
  }

  // Search and filters

  /** Keystroke handler — debounced, no immediate fetch */
  onSearch(): void {
    this.cdr.detectChanges();
    this.searchSubject.next(this.searchQuery);
  }

  selectCategory(name: string | null): void {
    this.selectedCategory = name;
    this.currentPage = 1;
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
    this.currentPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

  onAvailableToggle(): void {
    this.availableOnly = !this.availableOnly;
    this.currentPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

  onFreeToggle(): void {
    this.freeOnly = !this.freeOnly;
    this.currentPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1 },
      queryParamsHandling: 'merge',
    });
    this.loadItems();
  }

  // Favorites

  toggleFavorite(itemId: number, event: Event): void {
    event.stopPropagation();
    if (this.togglingIds.has(itemId)) return;
    this.togglingIds.add(itemId);

    const isFavorite = this.favoriteIds.has(itemId);
    this.favoriteService.toggle(itemId, false).subscribe({
      next: () => {
        isFavorite ? this.favoriteIds.delete(itemId) : this.favoriteIds.add(itemId);
        this.togglingIds.delete(itemId);
        this.cdr.detectChanges();
      },
      error: (error) => {
        if (error.status === 409) this.favoriteIds.add(itemId);
        else if (error.status === 404) this.favoriteIds.delete(itemId);
        this.showToast(error.error?.message ?? 'Something went wrong.');
        this.togglingIds.delete(itemId);
        this.cdr.detectChanges();
      }
    });
  }

  // Category strip drag

  onDragStart(e: MouseEvent) {
    this.isDragging = true;
    this.dragStartX = e.pageX;
    this.scrollStartX = this.categoryStrip.nativeElement.scrollLeft;
  }

  onDragMove(e: MouseEvent) {
    if (!this.isDragging) return;
    e.preventDefault();
    const delta = e.pageX - this.dragStartX;
    this.categoryStrip.nativeElement.scrollLeft = this.scrollStartX - delta;
  }

  onDragEnd() {
    this.isDragging = false;
  }

  scrollCategories(dir: 'left' | 'right'): void {
    this.categoryStrip.nativeElement.scrollBy({
      left: dir === 'right' ? 220 : -220,
      behavior: 'smooth',
    });
  }

  // Pagination

  get totalPages(): number {
    return getTotalPages(this.totalCount, this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page },
      queryParamsHandling: 'merge',
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadItems();
  }

  // Helpers

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getCategoryEmoji(name: string): string { return getCategoryEmoji(name); }
  getConditionClass(condition: ItemCondition): string { return getConditionClass(condition); }
  getAvailabilityClass(availability: ItemAvailability): string { return getAvailabilityClass(availability); }
  getAvailabilityLabel(availability: ItemAvailability): string { return getAvailabilityLabel(availability); }
  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();
    clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 3000);
  }
}