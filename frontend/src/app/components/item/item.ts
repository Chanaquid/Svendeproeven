import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { CreateItemDto, ItemListDto } from '../../dtos/itemDto';
import { CategoryDto } from '../../dtos/categoryDto';
import { ItemCondition, ItemAvailability } from '../../dtos/enums';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { CategoryService } from '../../services/categoryService';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Navbar } from '../navbar/navbar';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';

@Component({
  selector: 'app-item',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './item.html',
  styleUrl: './item.css',
})
export class Item implements OnInit, OnDestroy {
  allItems: ItemListDto[] = [];
  filteredItems: ItemListDto[] = [];

  isLoading = true;
  searchQuery = '';
  activeTab: 'all' | 'active' | 'pending' | 'rejected' | 'inactive' = 'all';

  showDeleteModal = false;
  itemToDelete: ItemListDto | null = null;
  isDeleting = false;

  tabs = [
    { key: 'all' as const, label: 'All' },
    { key: 'active' as const, label: 'Active' },
    { key: 'pending' as const, label: 'Pending' },
    { key: 'rejected' as const, label: 'Rejected' },
    { key: 'inactive' as const, label: 'Inactive' },
  ];

  // Add item
  showAddModal = false;
  isCreating = false;
  createError = '';
  categories: CategoryDto[] = [];
  createForm: CreateItemDto & { photoUrl?: string } = this.emptyCreateForm();
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;
  isAdmin = false;

  createdItemId: number | null = null;
  copiedSlug: string | null = null;

  // ── Pagination — dynamic page size matching window cols × 3 rows ──────────
  currentPage = 1;


  //Sorting
  sortLabel = 'newest';

  //Address api
  private readonly GEOAPIFY_KEY = '6efe16ed3bb047b8975d6f4738a471a9';

  get pageSize(): number {
    const cardMinWidth = 240;
    const gap = 16;
    const pagePadding = 80;
    const availableWidth = window.innerWidth - pagePadding;
    const cols = Math.max(1, Math.floor((availableWidth + gap) / (cardMinWidth + gap)));
    return cols * 3;
  }

  get pagedItems(): ItemListDto[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredItems.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return getTotalPages(this.filteredItems.length, this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page, tab: this.activeTab !== 'all' ? this.activeTab : null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.cdr.detectChanges();
  }

  // Recalculate on resize — page size changes with window width
  private resizeHandler = () => {
    this.currentPage = 1;
    this.cdr.detectChanges();
  };

  constructor(
    private authService: AuthService,
    private itemService: ItemService,
    private categoryService: CategoryService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone,
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.isAdmin = this.authService.isAdmin();

    // Restore page + tab from URL
    this.route.queryParams.subscribe(params => {
      this.activeTab = (params['tab'] as any) || 'all';
      this.currentPage = +params['page'] || 1;
    });

    window.addEventListener('resize', this.resizeHandler);

    this.loadItems();
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
  }

  emptyCreateForm(): CreateItemDto & { photoUrl?: string } {
    return {
      categoryId: 0,
      title: '',
      description: '',
      currentValue: 0,
      pricePerDay: 0,
      isFree: false,
      condition: ItemCondition.Good,
      minLoanDays: undefined,
      maxLoanDays: undefined,
      requiresVerification: false,
      pickupAddress: '',
      pickupLatitude: undefined as any,
      pickupLongitude: undefined as any,
      availableFrom: '',
      availableUntil: '',
      photoUrl: '',
    };
  }

  private loadItems(): void {
    this.isLoading = true;
    this.itemService.getMyItems({}, { page: 1, pageSize: 200, sortDescending: true }).subscribe({
      next: (res) => {
        this.allItems = res.data?.items ?? [];
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    let result = [...this.allItems];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.title.toLowerCase().includes(q) ||
        i.categoryName.toLowerCase().includes(q)
      );
    }

    switch (this.activeTab) {
      case 'active': result = result.filter(i => i.status === 'Approved' && i.isActive !== false); break;
      case 'pending': result = result.filter(i => i.status === 'Pending'); break;
      case 'rejected': result = result.filter(i => i.status === 'Rejected'); break;
      case 'inactive': result = result.filter(i => i.isActive === false); break;
    }


    // 3Sorting Logic
    switch (this.sortLabel) {
      case 'newest':
        result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        break;
      case 'oldest':
        result.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
        break;
      case 'az':
        result.sort((a, b) => a.title.localeCompare(b.title));
        break;
      case 'za':
        result.sort((a, b) => b.title.localeCompare(a.title));
        break;
      case 'price_asc':
        result.sort((a, b) => (a.pricePerDay || 0) - (b.pricePerDay || 0));
        break;
      case 'price_desc':
        result.sort((a, b) => (b.pricePerDay || 0) - (a.pricePerDay || 0));
        break;
      case 'rating':
        result.sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0));
        break;
    }

    this.filteredItems = result;
  }


  onSortChange(value: string): void {
    this.sortLabel = value;
    this.currentPage = 1;
    this.applyFilters();
    this.cdr.detectChanges();
  }

  onTabChange(key: typeof this.activeTab): void {
    this.activeTab = key;
    this.currentPage = 1;
    this.applyFilters();
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: key !== 'all' ? key : null, page: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.cdr.detectChanges();
  }

  onSearchChange(): void {
    this.currentPage = 1;
    this.applyFilters();
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.cdr.detectChanges();
  }

  getTabCount(key: string): number {
    switch (key) {
      case 'all': return this.allItems.length;
      case 'active': return this.allItems.filter(i => i.status === 'Approved' && i.isActive !== false).length;
      case 'pending': return this.allItems.filter(i => i.status === 'Pending').length;
      case 'rejected': return this.allItems.filter(i => i.status === 'Rejected').length;
      case 'inactive': return this.allItems.filter(i => i.isActive === false).length;
      default: return 0;
    }
  }

  get approvedCount() { return this.allItems.filter(i => i.status === 'Approved' && i.isActive !== false).length; }
  get pendingCount() { return this.allItems.filter(i => i.status === 'Pending').length; }
  get rejectedCount() { return this.allItems.filter(i => i.status === 'Rejected').length; }
  get inactiveCount() { return this.allItems.filter(i => i.isActive === false).length; }

  toggleActive(item: ItemListDto): void {
    this.itemService.toggleActive(item.id, { isActive: !item.isActive }).subscribe({
      next: () => {
        item.isActive = !item.isActive;
        this.applyFilters();
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to toggle active:', err),
    });
  }

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }

  confirmDelete(item: ItemListDto): void {
    this.itemToDelete = item;
    this.showDeleteModal = true;
  }

  deleteItem(): void {
    if (!this.itemToDelete) return;
    this.isDeleting = true;
    this.itemService.delete(this.itemToDelete.id).subscribe({
      next: () => {
        this.allItems = this.allItems.filter(i => i.id !== this.itemToDelete!.id);
        this.applyFilters();
        if (this.currentPage > this.totalPages && this.totalPages > 0) {
          this.currentPage = this.totalPages;
        }
        this.showDeleteModal = false;
        this.itemToDelete = null;
        this.isDeleting = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isDeleting = false;
        this.cdr.detectChanges();
      }
    });
  }

  getCategoryEmoji(cat: string): string { return getCategoryEmoji(cat); }
  getConditionClass(condition: ItemCondition): string { return getConditionClass(condition); }
  getAvailabilityClass(availability: ItemAvailability): string { return getAvailabilityClass(availability); }
  getAvailabilityLabel(availability: ItemAvailability): string { return getAvailabilityLabel(availability); }

  // "Approved" → shows as "Active" (green), never shows raw "Approved" text
  getStatusClass(item: ItemListDto): string {
    if (item.status === 'Approved' && item.isActive !== false) return 'status-active';
    if (item.status === 'Pending') return 'status-pending';
    if (item.status === 'Rejected') return 'status-rejected';
    return 'status-inactive';
  }

  getStatusLabel(item: ItemListDto): string {
    if (item.status === 'Approved' && item.isActive !== false) return 'Active';
    if (item.status === 'Approved' && item.isActive === false) return 'Inactive';
    return item.status;
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }

  openAddModal(): void {
    this.createForm = this.emptyCreateForm();
    this.createError = '';
    this.showAddModal = true;
    if (this.categories.length === 0) {
      this.categoryService.getAll().subscribe({
        next: (res) => {
          this.categories = res.data ?? [];
          this.cdr.detectChanges();
        }
      });
    }
  }

  createItem(): void {
    if (!this.createForm.title || !this.createForm.categoryId || !this.createForm.availableFrom || !this.createForm.availableUntil) {
      this.createError = 'Please fill in all required fields.';
      return;
    }
    this.isCreating = true;
    this.createError = '';
    const { photoUrl, ...itemDto } = this.createForm;
    this.itemService.create(itemDto).subscribe({
      next: (res) => {
        const created = res.data!;
        this.createdItemId = created.id;
        if (photoUrl?.trim()) {
          this.itemService.addPhoto(created.id, {
            photoUrl: photoUrl.trim(), isPrimary: true, displayOrder: 0,
          }).subscribe({
            next: () => this.finishCreate(),
            error: () => this.finishCreate(),
          });
        } else {
          this.finishCreate();
        }
      },
      error: (err) => {
        this.createError = err.error?.message ?? 'Failed to create item.';
        this.isCreating = false;
        this.cdr.detectChanges();
      }
    });
  }

  private finishCreate(): void {
    this.showAddModal = false;
    this.isCreating = false;
    this.cdr.detectChanges();
    this.loadItems();
  }

  onAddressInput(value: string): void {
    clearTimeout(this.addressSearchTimeout);
    this.showAddressSuggestions = false;
    this.addressSuggestions = [];

    if (!value || value.length < 3) return;

    this.addressSearchTimeout = setTimeout(() => {
      const url = `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(value)}&limit=5&apiKey=${this.GEOAPIFY_KEY}`;
      fetch(url)
        .then(res => res.json())
        .then(data => {
          this.ngZone.run(() => {
            this.addressSuggestions = data.features ?? [];
            this.showAddressSuggestions = this.addressSuggestions.length > 0;
            this.cdr.detectChanges();
          });
        });
    }, 400);
  }

  selectAddress(place: any): void {
    const props = place.properties;
    this.createForm.pickupAddress = props.formatted;
    this.createForm.pickupLatitude = place.geometry.coordinates[1];
    this.createForm.pickupLongitude = place.geometry.coordinates[0];
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.cdr.detectChanges();
  }

  copyShareLink(slug: string, event: Event): void {
    event.stopPropagation();
    const shareUrl = `${window.location.origin}/items/${slug}`;
    navigator.clipboard.writeText(shareUrl).then(() => {
      this.copiedSlug = slug;
      this.cdr.detectChanges();
      setTimeout(() => { this.copiedSlug = null; this.cdr.detectChanges(); }, 2000);
    }).catch(err => console.error('Could not copy text:', err));
  }
}