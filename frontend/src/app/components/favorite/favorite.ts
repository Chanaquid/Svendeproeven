import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/authService';
import { UserFavoriteService } from '../../services/userFavoriteService';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';
import { Navbar } from '../navbar/navbar';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { UserFavoriteItemListDto } from '../../dtos/userFavoriteItemDto';

@Component({
  selector: 'app-favorite',
  standalone: true,
  imports: [CommonModule, RouterLink, Navbar, FormsModule],
  templateUrl: './favorite.html',
  styleUrls: ['./favorite.css', '../home/home.css'],
})
export class Favorite implements OnInit {
  allFavorites: UserFavoriteItemListDto[] = [];
  favorites: UserFavoriteItemListDto[] = [];
  pagedFavorites: UserFavoriteItemListDto[] = [];
  isLoading = true;
  removingIds = new Set<number>();


  searchQuery = '';
  sortLabel = 'newest';
  availableOnly = false;
  freeOnly = false;

  currentPage = 1;
  readonly PAGE_SIZE = 12;

  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private favoriteService: UserFavoriteService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.searchSubject.pipe(
      debounceTime(300),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.applyFilters();
    });

    this.loadFavorites();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadFavorites(): void {
    this.isLoading = true;
    this.favoriteService.getMyFavorites({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        this.allFavorites = res.data?.items ?? [];
        console.log(this.allFavorites);
        this.isLoading = false;
        this.applyFilters();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    let result = [...this.allFavorites];

    // Search
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(f =>
        f.title.toLowerCase().includes(q) ||
        f.categoryName.toLowerCase().includes(q) ||
        f.ownerName.toLowerCase().includes(q) ||
        f.pickupAddress?.toLowerCase().includes(q)
      );
    }

    // Filters
    if (this.availableOnly) {
      result = result.filter(f => f.availability === ItemAvailability.Available);
    }
    if (this.freeOnly) {
      result = result.filter(f => f.isFree);
    }

    // Sort
    switch (this.sortLabel) {
      case 'newest': result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()); break;
      case 'oldest': result.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()); break;
      case 'rating': result.sort((a, b) => (b.averageRating ?? 0) - (a.averageRating ?? 0)); break;
      case 'az': result.sort((a, b) => a.title.localeCompare(b.title)); break;
      case 'za': result.sort((a, b) => b.title.localeCompare(a.title)); break;
      case 'price_asc':
        result = result.filter(f => !f.isFree);
        result.sort((a, b) => a.pricePerDay - b.pricePerDay);
        break;
      case 'price_desc':
        result = result.filter(f => !f.isFree);
        result.sort((a, b) => b.pricePerDay - a.pricePerDay);
        break;
    }

    this.favorites = result;
    this.updatePage();
    this.cdr.detectChanges();
  }

  onSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onSortChange(value: string): void {
    this.sortLabel = value;
    this.currentPage = 1;
    this.applyFilters();
  }

  onAvailableToggle(): void {
    this.availableOnly = !this.availableOnly;
    this.currentPage = 1;
    this.applyFilters();
  }

  onFreeToggle(): void {
    this.freeOnly = !this.freeOnly;
    this.currentPage = 1;
    this.applyFilters();
  }

  removeFavorite(itemId: number, event: Event): void {
    event.stopPropagation();
    this.removingIds.add(itemId);
    this.favoriteService.toggle(itemId).subscribe({
      next: (res) => {
        if (!res.data?.isFavorited) {
          this.allFavorites = this.allFavorites.filter(f => f.id !== itemId);
          this.applyFilters();
        }
        this.removingIds.delete(itemId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.removingIds.delete(itemId);
        this.cdr.detectChanges();
      }
    });
  }

  private updatePage(): void {
    const start = (this.currentPage - 1) * this.PAGE_SIZE;
    this.pagedFavorites = this.favorites.slice(start, start + this.PAGE_SIZE);
  }

  get totalPages(): number { return getTotalPages(this.favorites.length, this.PAGE_SIZE); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.updatePage();
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.cdr.detectChanges();
  }

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getCategoryEmoji(name: string): string { return getCategoryEmoji(name); }
  getConditionClass(condition: ItemCondition): string { return getConditionClass(condition); }
  getAvailabilityClass(availability: ItemAvailability): string { return getAvailabilityClass(availability); }
  getAvailabilityLabel(availability: ItemAvailability): string { return getAvailabilityLabel(availability); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) || '??';
  }
}