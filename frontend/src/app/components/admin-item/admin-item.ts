import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Navbar } from '../navbar/navbar';
import { ItemDto, ItemListDto, UpdateItemDto } from '../../dtos/itemDto';
import { ItemHistoryDto } from '../../dtos/adminDto';
import { CategoryDto } from '../../dtos/categoryDto';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { CategoryService } from '../../services/categoryService';
import { AdminService } from '../../services/adminService';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import { ItemAvailability } from '../../dtos/enums';

@Component({
  selector: 'app-admin-item',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-item.html',
  styleUrl: './admin-item.css',
})
export class AdminItem implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadItems(); };
  public ItemAvailability = ItemAvailability;
  // List state
  filteredItems: ItemListDto[] = [];
  isLoading = true;
  listLoading = false;
  searchQuery = '';
  activeTab: 'approved' | 'pending' | 'rejected' = 'pending';

  // Pagination
  currentPage = 1;
  totalCount = 0;

  // Tab counts
  approvedCount = 0;
  pendingCount = 0;
  rejectedCount = 0;

  // History / modal
  itemHistory: ItemHistoryDto | null = null;
  visibleReviews = 5;
  visibleLoans = 5;

  // Modal
  showItemModal = false;
  selectedItem: ItemListDto | null = null;
  itemDetail: ItemDto | null = null;
  adminNote = '';
  isActioning = false;
  actionError = '';
  actionSuccess = '';
  selectedPhoto: string | null = null;

  // Edit
  showEditForm = false;
  editForm: UpdateItemDto | null = null;
  isUpdating = false;
  updateError = '';
  updateSuccess = '';


  // Loan pagination
  loansCurrentPage = 1;
  loansPageSize = 5;
  loansTotalCount = 0;
  loansLoading = false;
  allLoans: any[] = [];

  // Review pagination  
  reviewsCurrentPage = 1;
  reviewsPageSize = 5;

  tabs = [
    { key: 'approved' as const, label: 'Approved' },
    { key: 'pending'  as const, label: 'Pending'  },
    { key: 'rejected' as const, label: 'Rejected' },
  ];

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', gaming: '🎮',
    gardening: '🌱', biking: '🚲', kitchen: '🍳', cleaning: '🧹',
    fashion: '👗', art: '🎨', baby: '👶', events: '🎉', auto: '🚗', other: '📦',
  };

  constructor(
    private authService: AuthService,
    private itemService: ItemService,
    private categoryService: CategoryService,
    private adminService: AdminService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Dynamic page size ────────────────────────────────────────────────────────

  get pageSize(): number {
    const cardMinWidth = 240;
    const gap = 16;
    const pagePadding = 80;
    const availableWidth = window.innerWidth - pagePadding;
    const cols = Math.max(1, Math.floor((availableWidth + gap) / (cardMinWidth + gap)));
    return cols * 3;
  }

  sortLabel = 'newest';
  sortBy = 'createdAt';
  sortDescending = true;
  selectedCategory: string | null = null;
  availableOnly = false;
  freeOnly = false;


  get totalPages(): number {
    return getTotalPages(this.totalCount, this.pageSize);
  }

  get pageNumbers(): number[] {
    return getPageNumbers(this.currentPage, this.totalPages);
  }


  private categoryIdMap: Record<string, number> = {
    'Electronics': 1, 'Tools': 2, 'Sports': 3, 'Music': 4,
    'Books': 5, 'Camping': 6, 'Photography': 7, 'Gaming': 8,
    'Gardening': 9, 'Biking': 10, 'Kitchen': 11, 'Cleaning': 12,
    'Fashion': 13, 'Art': 14, 'Baby': 15, 'Events': 16,
    'Auto': 17, 'Other': 18,
  };

  categories: (CategoryDto | { icon: string | null; name: string; id?: number })[] = [
    { icon: '📱', name: 'Electronics', id: 1 },
    { icon: '🔧', name: 'Tools', id: 2 },
    { icon: '⚽', name: 'Sports', id: 3 },
    { icon: '🎸', name: 'Music', id: 4 },
    { icon: '📚', name: 'Books', id: 5 },
    { icon: '⛺', name: 'Camping', id: 6 },
    { icon: '📷', name: 'Photography', id: 7 },
    { icon: '🎮', name: 'Gaming', id: 8 },
    { icon: '🌱', name: 'Gardening', id: 9 },
    { icon: '🚲', name: 'Biking', id: 10 },
    { icon: '🍳', name: 'Kitchen', id: 11 },
    { icon: '🧹', name: 'Cleaning', id: 12 },
    { icon: '👗', name: 'Fashion', id: 13 },
    { icon: '🎨', name: 'Art', id: 14 },
    { icon: '👶', name: 'Baby', id: 15 },
    { icon: '🎉', name: 'Events', id: 16 },
    { icon: '🚗', name: 'Auto', id: 17 },
    { icon: '📦', name: 'Other', id: 18 },
  ];

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadItems();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadItems();
    });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Load ─────────────────────────────────────────────────────────────────────

  private loadItems(): void {
    this.listLoading = true;
  const statusMap = { approved: 'Approved', pending: 'Pending', rejected: 'Rejected' } as const;

    const filter = {
    status: statusMap[this.activeTab] as any,
    search: this.searchQuery.trim() || null,
    ...(this.selectedCategory && { categoryId: this.categoryIdMap[this.selectedCategory] }),
    ...(this.availableOnly && { availability: ItemAvailability.Available }),
    ...(this.freeOnly && { isFree: true }),
  };

    const request = {
    page: this.currentPage,
    pageSize: Math.max(this.pageSize, 1),
    sortBy: this.sortBy,
    sortDescending: this.sortDescending,
  };

    this.adminService.getAllItems(filter, request)
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (result) => {
        this.filteredItems = result.items ?? result;
        this.totalCount = result.totalCount ?? this.filteredItems.length;
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.listLoading = false; this.isLoading = false; this.cdr.detectChanges(); },
    });
  }

  private loadTabCounts(): void {
    const request = { pageNumber: 1, pageSize: 1 };

    this.adminService.getAllItems({ status: 'Approved' as any, createdAfter: null }, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (r) => { this.approvedCount = r.totalCount ?? 0; this.cdr.detectChanges(); } });

    this.adminService.getAllItems({ status: 'Pending' as any, createdAfter: null }, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (r) => { this.pendingCount = r.totalCount ?? 0; this.cdr.detectChanges(); } });

    this.adminService.getAllItems({ status: 'Rejected' as any, createdAfter: null }, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (r) => { this.rejectedCount = r.totalCount ?? 0; this.cdr.detectChanges(); } });
  }

  // ─── Tabs / search / pagination ───────────────────────────────────────────────

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
  this.loadItems();
  }

  selectCategory(name: string | null): void {
    this.selectedCategory = name;
    this.currentPage = 1;
    this.loadItems();
  }

  onAvailableToggle(): void { this.availableOnly = !this.availableOnly; this.currentPage = 1; this.loadItems(); }
  onFreeToggle(): void      { this.freeOnly = !this.freeOnly;           this.currentPage = 1; this.loadItems(); }


  get paginatedLoans() {
    const start = (this.loansCurrentPage - 1) * this.loansPageSize;
    return this.allLoans.slice(start, start + this.loansPageSize);
  }

  get loansTotalPages(): number { return getTotalPages(this.allLoans.length, this.loansPageSize); }

  get paginatedReviews() {
    if (!this.itemHistory) return [];
    const start = (this.reviewsCurrentPage - 1) * this.reviewsPageSize;
    return this.itemHistory.reviews.slice(start, start + this.reviewsPageSize);
  }

  get reviewsTotalPages(): number {
    return getTotalPages(this.itemHistory?.reviewCount ?? 0, this.reviewsPageSize);
  }

  loansGoToPage(p: number): void { if (p < 1 || p > this.loansTotalPages) return; this.loansCurrentPage = p; this.cdr.detectChanges(); }
  reviewsGoToPage(p: number): void { if (p < 1 || p > this.reviewsTotalPages) return; this.reviewsCurrentPage = p; this.cdr.detectChanges(); }


  switchTab(tab: 'approved' | 'pending' | 'rejected'): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadItems();
  }

  onSearch(): void {
    this.searchSubject.next(this.searchQuery);
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadItems();
  }

  getTabCount(key: string): number {
    if (key === 'approved') return this.approvedCount;
    if (key === 'pending')  return this.pendingCount;
    if (key === 'rejected') return this.rejectedCount;
    return 0;
  }

  // ─── Modal ────────────────────────────────────────────────────────────────────

  openItemModal(item: ItemListDto): void {
    this.selectedItem = item;
    this.itemDetail = null;
    this.itemHistory = null;
    this.allLoans = [];
    this.loansCurrentPage = 1;
    this.reviewsCurrentPage = 1;
    this.adminNote = '';
    this.actionError = '';
    this.actionSuccess = '';
    this.showEditForm = false;
    this.showItemModal = true;

    this.adminService.getItemById(item.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (detail) => {
          this.itemDetail = detail;
          this.adminNote = detail.adminNote ?? '';
          this.cdr.detectChanges();
        },
      });

    this.adminService.getAllLoans({ itemId: item.id }, { pageNumber: 1, pageSize: 200 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.allLoans = (result.items ?? result).sort(
            (a: any, b: any) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
          );



          this.itemHistory = {
            loans: this.allLoans,
            reviews: [],
            reviewCount: 0,
            averageRating: 0,
          } as unknown as ItemHistoryDto;
          this.cdr.detectChanges();
        },
      });
  }

  // ─── Status change ────────────────────────────────────────────────────────────

  changeStatus(status: 'Approved' | 'Pending' | 'Rejected'): void {
    if (!this.selectedItem) return;

    if (this.itemDetail?.status === status) return;

    if (status === 'Rejected' && !this.adminNote.trim()) {
      this.actionError = 'A note is required when rejecting an item.';
      return;
    }

    this.isActioning = true;
    this.actionError = '';
    this.actionSuccess = '';

    // itemService.adminUpdateStatus returns Observable<ApiResponse<ItemDto>>
    this.itemService.adminUpdateStatus(this.selectedItem.id, {
      status: status as any,
      adminNote: this.adminNote.trim() || undefined,
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const updated = res.data!;
          this.itemDetail = updated;
          this.selectedItem!.status = updated.status;
          this.actionSuccess = `Status updated to ${status}.`;
          this.isActioning = false;
          this.loadItems();
          this.loadTabCounts();
          this.cdr.detectChanges();
          setTimeout(() => { this.actionSuccess = ''; this.cdr.detectChanges(); }, 3000);
        },
        error: (err) => {
          this.actionError = err.error?.message ?? 'Failed to update status.';
          this.isActioning = false;
          this.cdr.detectChanges();
        },
      });
  }

  // ─── Edit ─────────────────────────────────────────────────────────────────────

  openEditForm(): void {
    if (!this.itemDetail) return;
    this.editForm = {
      categoryId: this.itemDetail.categoryId,
      title: this.itemDetail.title,
      description: this.itemDetail.description,
      currentValue: this.itemDetail.currentValue,
      condition: this.itemDetail.condition as any,
      minLoanDays: this.itemDetail.minLoanDays,
      requiresVerification: this.itemDetail.requiresVerification,
      pickupAddress: this.itemDetail.pickupAddress,
      pickupLatitude: this.itemDetail.pickupLatitude,
      pickupLongitude: this.itemDetail.pickupLongitude,
      availableFrom: this.itemDetail.availableFrom?.split('T')[0],
      availableUntil: this.itemDetail.availableUntil?.split('T')[0],
      isActive: this.itemDetail.isActive,
    };
    this.showEditForm = true;
    this.updateError = '';
    this.updateSuccess = '';

    if (this.categories.length === 0) {
      // categoryService.getAll() returns Observable<ApiResponse<CategoryDto[]>>
      this.categoryService.getAll()
        .pipe(takeUntil(this.destroy$))
        .subscribe((res) => {
          this.categories = res.data ?? [];
          this.cdr.detectChanges();
        });
    }
  }

  updateItem(): void {
    if (!this.selectedItem || !this.editForm) return;
    this.isUpdating = true;
    this.updateError = '';
    this.updateSuccess = '';

    // itemService.update returns Observable<ApiResponse<ItemDto>>
    this.itemService.update(this.selectedItem.id, this.editForm)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const updated = res.data!;
          this.itemDetail = updated;
          this.selectedItem!.title = updated.title;
          this.selectedItem!.status = updated.status;
          this.selectedItem!.isActive = updated.isActive;
          this.isUpdating = false;
          this.updateSuccess = 'Item updated successfully.';
          this.showEditForm = false;
          this.loadItems();
          this.cdr.detectChanges();
          setTimeout(() => { this.updateSuccess = ''; this.cdr.detectChanges(); }, 3000);
        },
        error: (err) => {
          this.updateError = err.error?.message ?? 'Failed to update item.';
          this.isUpdating = false;
          this.cdr.detectChanges();
        },
      });
  }

  // ─── UI helpers ───────────────────────────────────────────────────────────────

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat?.toLowerCase()] ?? '📦';
  }

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':       return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved':     return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'returned':     return 'bg-cyan-600 text-white border-zinc-400/20';
      case 'late':         return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'pending':      return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending': return 'bg-indigo-400/10 text-indigo-400 border-indigo-400/20';
      case 'cancelled':    return 'bg-zinc-700/50 text-zinc-500 border-zinc-600/50';
      case 'rejected':     return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:             return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }


  get displayedReviews() { return this.paginatedReviews; }
  get displayedLoans()   { return this.paginatedLoans; }
}