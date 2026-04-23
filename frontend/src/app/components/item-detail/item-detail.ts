import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { ItemReviewService } from '../../services/itemReviewService';
import { UserService } from '../../services/userService';
import { ItemDto, UpdateItemDto } from '../../dtos/itemDto';
import { ItemReviewDto } from '../../dtos/itemReviewDto';
import { ItemPhotoDto } from '../../dtos/itemPhotoDto';
import { Navbar } from '../navbar/navbar';
import { LoanService } from '../../services/loanService';
import { LoanDto, LoanListDto } from '../../dtos/loanDto';
import { DisputeService } from '../../services/disputeService';
import { DisputeListDto } from '../../dtos/disputeDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { getConditionClass, getCategoryEmoji } from '../../utils/item.utils';
import { ItemCondition, ItemAvailability, ReportReason, ReportType } from '../../dtos/enums';
import { ReportService } from '../../services/reportService';
import { UserFavoriteService } from '../../services/userFavoriteService';
import { UploadImageService } from '../../services/uploadImageService';

interface EditablePhoto {
  id?: number;          // undefined = newly uploaded, not yet saved
  photoUrl: string;
  isPrimary: boolean;
  displayOrder: number;
  file?: File;          // present only for new uploads before save
  uploading?: boolean;
}

@Component({
  selector: 'app-item-detail',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './item-detail.html',
  styleUrl: './item-detail.css',
})
export class ItemDetail implements OnInit {

  item: ItemDto | null = null;
  reviews: ItemReviewDto[] = [];
  isLoading = true;
  isLoadingReviews = true;
  selectedPhoto: string | null = null;
  currentUserId = '';
  isAdmin = false;
  isVerified = false;

  // Loan request
  showLoanModal = false;
  loanForm = { startDate: '', endDate: '' };
  isRequesting = false;
  loanError = '';
  loanSuccess = '';
  existingLoan: LoanDto | null = null;
  isCancellingLoan = false;
  cancelError = '';

  // Loan history (owner view)
  loanHistory: LoanListDto[] = [];
  isLoadingHistory = false;
  historyCollapsed = false;
  visibleLoans = 5;

  // Dispute history (owner view)
  disputeHistory: DisputeListDto[] = [];
  isLoadingDisputes = false;
  disputesCollapsed = false;
  visibleDisputes = 5;

  // Edit modal
  showEditModal = false;
  editForm = {
    title: '',
    description: '',
    condition: 'Good' as unknown as UpdateItemDto['condition'],
    currentValue: 0,
    availableFrom: '',
    availableUntil: '',
    minLoanDays: undefined as number | undefined,
    maxLoanDays: undefined as number | undefined,
    pickupAddress: '',
    pickupLatitude: 0 as number,
    pickupLongitude: 0 as number,
    requiresVerification: false,
    isActive: true,
  };
  isSavingEdit = false;
  editError = '';
  editSuccess = '';
  descriptionExpanded = false;

  // Photo management in edit modal
  editPhotos: EditablePhoto[] = [];
  isDraggingOver: number | null = null;
  dragSourceIndex: number | null = null;

  // Address autocomplete (Geoapify — same as register)
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;

  // Report item
  showReportModal = false;
  reportReason: ReportReason | '' = '';
  reportDetails = '';
  isSubmittingReport = false;
  reportSuccess = '';
  reportError = '';

  // Favourite
  isFavorited = false;
  isTogglingFavorite = false;

  //photos
  activePhotoIndex = 0;

  // Reviews
  visibleReviews = 5;
  get displayedReviews() { return this.reviews.slice(0, this.visibleReviews); }
  loadMoreReviews() { this.visibleReviews += 5; }

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10;
  }

  get isOwner(): boolean {
    return !!this.item && this.item.ownerId === this.currentUserId;
  }

  get canRequestLoan(): boolean {
    if (!this.item) return false;
    return this.item.status === 'Approved' &&
      this.item.isActive &&
      !this.item.isCurrentlyOnLoan &&
      !this.isOwner &&
      !this.existingLoan &&
      (!this.item.requiresVerification || this.isVerified);
  }

  get hasActiveLoanRequest(): boolean {
    if (!this.existingLoan) return false;
    return ['Pending', 'AdminPending', 'Approved', 'Active', 'Late'].includes(this.existingLoan.status);
  }

  get displayedDisputes() { return this.disputeHistory.slice(0, this.visibleDisputes); }
  get displayedLoanHistory() { return this.loanHistory.slice(0, this.visibleLoans); }
  loadMoreDisputes() { this.visibleDisputes += 5; }
  loadMoreLoanHistory() { this.visibleLoans += 5; }

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private authService: AuthService,
    private itemService: ItemService,
    private reviewService: ItemReviewService,
    private userService: UserService,
    private loanService: LoanService,
    private reportService: ReportService,
    private disputeService: DisputeService,
    private favoriteService: UserFavoriteService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();

    if (this.authService.isLoggedIn()) {
      this.userService.getMyProfile().subscribe({
        next: (res) => {
          this.currentUserId = res.data?.id ?? '';
          this.isVerified = res.data?.isVerified ?? false;
          this.cdr.detectChanges();
        },
        error: () => { }
      });
    }

    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.loadItem(slug);
  }

  // ── Data loading ──────────────────────────────────────────────

  private loadItem(slug: string): void {
    this.isLoading = true;
    this.itemService.getBySlug(slug).subscribe({
      next: (res) => {
        this.item = res.data ?? null;
        this.isLoading = false;
        this.cdr.detectChanges();

        if (!this.item) return;

        this.loadReviews(this.item.id);

        if (this.authService.isLoggedIn() && !this.isOwner) {
          this.checkExistingLoan();
          this.loadFavoriteStatus(this.item.id);
        }

        if (this.isOwner || this.isAdmin) {
          this.loadLoanHistory(this.item.id);
          this.loadDisputeHistory(this.item.id);
          this.resetPhotoIndex();
        }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }



  private resetPhotoIndex(): void {
    this.activePhotoIndex = 0;
  }

  prevPhoto(total: number): void {
    this.activePhotoIndex = (this.activePhotoIndex - 1 + total) % total;
  }
  
  nextPhoto(total: number): void {
    this.activePhotoIndex = (this.activePhotoIndex + 1) % total;
  }



  private checkExistingLoan(): void {
    if (!this.item) return;
    this.loanService.getMyLoanForItem(this.item.id).subscribe({
      next: (res) => {
        if (res.data) this.existingLoan = res.data;
        this.cdr.detectChanges();
      },
      error: () => { }
    });
  }

  private loadFavoriteStatus(itemId: number): void {
    this.favoriteService.getStatus(itemId).subscribe({
      next: (res) => {
        this.isFavorited = res.data?.isFavorited ?? false;
        this.cdr.detectChanges();
      },
      error: () => { }
    });
  }

  toggleFavorite(): void {
    if (!this.item || this.isTogglingFavorite) return;
    this.isTogglingFavorite = true;
    this.favoriteService.toggle(this.item.id).subscribe({
      next: (res) => {
        this.isFavorited = res.data?.isFavorited ?? !this.isFavorited;
        this.isTogglingFavorite = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isTogglingFavorite = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadReviews(itemId: number): void {
    this.isLoadingReviews = true;
    const request: PagedRequest = { page: 1, pageSize: 50 };
    this.reviewService.getByItem(itemId, {}, request).subscribe({
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

  private loadLoanHistory(itemId: number): void {
    this.isLoadingHistory = true;
    const filter = { itemId };
    const request: PagedRequest = { page: 1, pageSize: 50 };
    this.loanService.getMyAsLender(filter, request).subscribe({
      next: (res) => {
        this.loanHistory = (res.data?.items ?? []).filter(l => l.itemId === itemId);
        this.isLoadingHistory = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingHistory = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadDisputeHistory(itemId: number): void {
    this.isLoadingDisputes = true;
    const filter = {};
    const request: PagedRequest = { page: 1, pageSize: 50 };
    this.disputeService.adminGetHistoryByItem(itemId, filter, request).subscribe({
      next: (res) => {
        this.disputeHistory = res.data?.items ?? [];
        this.isLoadingDisputes = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.disputeService.getMyAll(filter, request).subscribe({
          next: (res2) => {
            this.disputeHistory = (res2.data?.items ?? []).filter((d: any) => d.itemId === itemId);
            this.isLoadingDisputes = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoadingDisputes = false;
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  // ── Loan request ──────────────────────────────────────────────

  requestLoan(): void {
    if (!this.item || !this.loanForm.startDate || !this.loanForm.endDate) {
      this.loanError = 'Please fill in both dates.';
      return;
    }
    this.isRequesting = true;
    this.loanError = '';

    this.loanService.create({
      itemId: this.item.id,
      startDate: this.loanForm.startDate,
      endDate: this.loanForm.endDate,
    }).subscribe({
      next: () => {
        this.isRequesting = false;
        this.loanSuccess = '✓ Loan request sent!';
        this.cdr.detectChanges();
        setTimeout(() => {
          this.showLoanModal = false;
          const slug = this.route.snapshot.paramMap.get('slug')!;
          this.loadItem(slug);
          this.cdr.detectChanges();
        }, 1000);
      },
      error: (err) => {
        this.loanError = err.error?.message ?? 'Failed to send request.';
        this.isRequesting = false;
        this.cdr.detectChanges();
      }
    });
  }

  cancelLoan(): void {
    if (!this.existingLoan) return;
    this.isCancellingLoan = true;
    this.cancelError = '';

    this.loanService.cancel(this.existingLoan.id, {
      loanId: this.existingLoan.id,
      reason: ''
    }).subscribe({
      next: () => {
        this.existingLoan = null;
        this.isCancellingLoan = false;
        const slug = this.route.snapshot.paramMap.get('slug')!;
        this.loadItem(slug);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.cancelError = err.error?.message ?? 'Failed to cancel loan.';
        this.isCancellingLoan = false;
        this.cdr.detectChanges();
      }
    });
  }

  openLoanModal(): void {
    if (!this.item) return;

    const availableFrom = new Date(this.item.availableFrom);
    const today = new Date();
    today.setHours(12, 0, 0, 0);

    const effectiveStart = availableFrom > today ? availableFrom : today;
    const startDate = this.toDateInputValue(effectiveStart);

    const minDays = this.item.minLoanDays ?? 1;
    const endDate = new Date(effectiveStart);
    endDate.setDate(endDate.getDate() + minDays);

    this.loanForm = {
      startDate,
      endDate: this.toDateInputValue(endDate),
    };
    this.loanError = '';
    this.loanSuccess = '';
    this.showLoanModal = true;
  }

  // ── Edit modal ────────────────────────────────────────────────

  openEditModal(): void {
    if (!this.item) return;
    this.editForm = {
      title: this.item.title ?? '',
      description: this.item.description ?? '',
      condition: (this.item.condition ?? 'Good') as unknown as UpdateItemDto['condition'],
      currentValue: this.item.currentValue ?? 0,
      availableFrom: this.toDateInputValue(this.item.availableFrom),
      availableUntil: this.toDateInputValue(this.item.availableUntil),
      minLoanDays: this.item.minLoanDays ?? undefined,
      maxLoanDays: this.editForm.maxLoanDays,
      pickupAddress: this.item.pickupAddress ?? '',
      pickupLatitude: this.item.pickupLatitude ?? 0,
      pickupLongitude: this.item.pickupLongitude ?? 0,
      requiresVerification: this.item.requiresVerification ?? false,
      isActive: this.item.isActive ?? true,
    };

    // Populate editable photos from existing item photos, sorted by displayOrder
    this.editPhotos = (this.item.photos ?? [])
      .slice()
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((p, i) => ({
        id: p.id,
        photoUrl: p.photoUrl,
        isPrimary: i === 0,
        displayOrder: i,
      }));

    this.editError = '';
    this.editSuccess = '';
    this.descriptionExpanded = false;
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.showEditModal = true;
  }

  closeEditModal(): void {
    if (this.isSavingEdit) return;
    this.showEditModal = false;
    this.showAddressSuggestions = false;
  }

  // ── Photo management ──────────────────────────────────────────

  async onPhotosSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    for (const file of files) {
      if (file.size > 8 * 1024 * 1024) {
        this.editError = `${file.name} exceeds 8 MB limit.`;
        continue;
      }
      const placeholder: EditablePhoto = {
        photoUrl: URL.createObjectURL(file),
        isPrimary: this.editPhotos.length === 0,
        displayOrder: this.editPhotos.length,
        file,
        uploading: true,
      };
      this.editPhotos = [...this.editPhotos, placeholder];
      this.cdr.detectChanges();

      try {
        const url = await this.uploadService.uploadImage(file);
        const idx = this.editPhotos.indexOf(placeholder);
        if (idx !== -1) {
          this.editPhotos[idx] = { ...this.editPhotos[idx], photoUrl: url, uploading: false, file: undefined };
          this.editPhotos = [...this.editPhotos];
        }
      } catch {
        this.editPhotos = this.editPhotos.filter(p => p !== placeholder);
        this.editError = `Failed to upload ${file.name}.`;
      }
      this.cdr.detectChanges();
    }
  }

  removeEditPhoto(index: number): void {
    this.editPhotos = this.editPhotos.filter((_, i) => i !== index);
    this.recalcPhotoOrder();
  }

  private recalcPhotoOrder(): void {
    this.editPhotos = this.editPhotos.map((p, i) => ({
      ...p,
      displayOrder: i,
      isPrimary: i === 0,
    }));
  }

  // ── Drag-and-drop reorder ────────────────────────────────────

  onDragStart(index: number): void {
    this.dragSourceIndex = index;
  }

  onDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    this.isDraggingOver = index;
  }

  onDragLeave(): void {
    this.isDraggingOver = null;
  }

  onDrop(event: DragEvent, targetIndex: number): void {
    event.preventDefault();
    if (this.dragSourceIndex === null || this.dragSourceIndex === targetIndex) {
      this.isDraggingOver = null;
      this.dragSourceIndex = null;
      return;
    }

    const photos = [...this.editPhotos];
    const [moved] = photos.splice(this.dragSourceIndex, 1);
    photos.splice(targetIndex, 0, moved);
    this.editPhotos = photos;
    this.recalcPhotoOrder();

    this.isDraggingOver = null;
    this.dragSourceIndex = null;
  }

  onDragEnd(): void {
    this.isDraggingOver = null;
    this.dragSourceIndex = null;
  }

  // ── Save edit ─────────────────────────────────────────────────

  saveEdit(): void {
    if (!this.item) return;
    if (!this.editForm.title?.trim()) {
      this.editError = 'Title is required.';
      return;
    }
    if (this.editPhotos.some(p => p.uploading)) {
      this.editError = 'Please wait for all photos to finish uploading.';
      return;
    }

    this.isSavingEdit = true;
    this.editError = '';
    this.editSuccess = '';

    // Derive availability from isActive toggle
    const availability: ItemAvailability = this.editForm.isActive
      ? ItemAvailability.Available
      : ItemAvailability.Unavailable;

    this.itemService.update(this.item.id, {
      title: this.editForm.title.trim(),
      description: this.editForm.description,
      condition: this.editForm.condition,
      currentValue: this.editForm.currentValue,
      availableFrom: this.editForm.availableFrom,
      availableUntil: this.editForm.availableUntil,
      minLoanDays: this.editForm.minLoanDays,
      pickupAddress: this.editForm.pickupAddress,
      pickupLatitude: this.editForm.pickupLatitude,
      pickupLongitude: this.editForm.pickupLongitude,
      requiresVerification: this.editForm.requiresVerification,
      isActive: this.editForm.isActive,
      availability,
    }).subscribe({
      next: (res) => {
        if (res.data) {
          this.syncPhotos(res.data);
        } else {
          this.isSavingEdit = false;
          this.editError = 'Unexpected response from server.';
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.editError = err.error?.message ?? 'Failed to save changes.';
        this.isSavingEdit = false;
        this.cdr.detectChanges();
      }
    });
  }


  private async syncPhotos(updatedItem: ItemDto): Promise<void> {
    const itemId = updatedItem.id;

    // Delete ALL existing photos first
    for (const photo of updatedItem.photos ?? []) {
      try { await this.itemService.deletePhoto(itemId, photo.id).toPromise(); } catch { /* best-effort */ }
    }

    // Re-add all photos in the user's chosen order (index 0 = primary)
    for (let i = 0; i < this.editPhotos.length; i++) {
      const photo = this.editPhotos[i];
      try {
        await this.itemService.addPhoto(itemId, {
          photoUrl: photo.photoUrl,
          isPrimary: i === 0,
          displayOrder: i,
        }).toPromise();
      } catch { /* best-effort */ }
    }

    // Refresh item to get new photo ids, then explicitly set primary
    this.itemService.getById(itemId).subscribe({
      next: async (res) => {
        const freshItem = res.data ?? updatedItem;
        
        // Set primary to the first photo
        const firstPhoto = freshItem.photos
          ?.slice()
          .sort((a, b) => a.displayOrder - b.displayOrder)[0];
        
        if (firstPhoto?.id) {
          try {
            await this.itemService.setPrimaryPhoto(itemId, firstPhoto.id).toPromise();
          } catch { /* best-effort */ }
        }

        // Final refresh
        this.itemService.getById(itemId).subscribe({
          next: (final) => {
            this.item = final.data ?? freshItem;
            this.isSavingEdit = false;
            this.editSuccess = '✓ Changes saved!';
            this.cdr.detectChanges();
            setTimeout(() => {
              this.showEditModal = false;
              this.editSuccess = '';
              if (this.item?.slug) {
                this.router.navigate(['/items', this.item.slug], { replaceUrl: true });
              }
              this.cdr.detectChanges();
            }, 1200);
          },
          error: () => {
            this.item = freshItem;
            this.isSavingEdit = false;
            this.editSuccess = '✓ Changes saved!';
            this.cdr.detectChanges();
            setTimeout(() => { this.showEditModal = false; }, 1200);
          }
        });
      },
      error: () => {
        this.isSavingEdit = false;
        this.editSuccess = '✓ Changes saved!';
        this.cdr.detectChanges();
        setTimeout(() => { this.showEditModal = false; }, 1200);
      }
    });
  }


  // ── Address autocomplete (Geoapify — same as register) ────────

  onAddressInput(value: string): void {
    clearTimeout(this.addressSearchTimeout);
    this.showAddressSuggestions = false;
    if (!value || value.length < 3) { this.addressSuggestions = []; return; }

    this.addressSearchTimeout = setTimeout(() => {
      const apiKey = '6efe16ed3bb047b8975d6f4738a471a9';
      const url = `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(value)}&limit=5&apiKey=${apiKey}`;

      fetch(url)
        .then(res => res.json())
        .then(data => {
          this.addressSuggestions = data.features ?? [];
          this.showAddressSuggestions = true;
          this.cdr.detectChanges();
        })
        .catch(() => { });
    }, 400);
  }

  selectAddress(suggestion: any): void {
    const props = suggestion.properties;
    this.editForm.pickupAddress = props.formatted;
    this.editForm.pickupLatitude = suggestion.geometry.coordinates[1];
    this.editForm.pickupLongitude = suggestion.geometry.coordinates[0];
    this.showAddressSuggestions = false;
    this.addressSuggestions = [];
    this.cdr.detectChanges();
  }

  // ── Report ────────────────────────────────────────────────────

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
      type: ReportType.Item,
      targetId: this.item!.id.toString(),
      reasons: this.reportReason as ReportReason,
      additionalDetails: this.reportDetails.trim() || null,
    }).subscribe({
      next: () => {
        this.isSubmittingReport = false;
        this.reportSuccess = '✓ Report submitted. Thank you.';
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

  // ── Loan price helpers ────────────────────────────────────────

  get totalLoanPrice(): number | null {
    if (!this.item || this.item.isFree) return null;
    if (!this.loanForm.startDate || !this.loanForm.endDate) return null;
    const start = new Date(this.loanForm.startDate);
    const end   = new Date(this.loanForm.endDate);
    const days  = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    if (days <= 0) return null;
    return days * this.item.pricePerDay;
  }

  get loanDays(): number {
    if (!this.loanForm.startDate || !this.loanForm.endDate) return 0;
    const start = new Date(this.loanForm.startDate);
    const end   = new Date(this.loanForm.endDate);
    return Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
  }

  // ── Generic helpers ───────────────────────────────────────────

  private toDateInputValue(value: string | Date | null | undefined): string {
    if (!value) return '';
    if (typeof value === 'string') return value.slice(0, 10);
    const y = value.getFullYear();
    const m = String(value.getMonth() + 1).padStart(2, '0');
    const d = String(value.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  getCategoryEmoji(cat: string): string { return getCategoryEmoji(cat); }
  getConditionClass(condition: string): string { return getConditionClass(condition as ItemCondition); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
  goBack(): void { window.history.back(); }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':       return 'history-status-tag--active';
      case 'approved':     return 'history-status-tag--approved';
      case 'late':         return 'history-status-tag--late';
      case 'pending':      return 'history-status-tag--pending';
      case 'adminpending': return 'history-status-tag--adminpending';
      case 'cancelled':    return 'history-status-tag--cancelled';
      case 'rejected':     return 'history-status-tag--rejected';
      case 'completed':    return 'history-status-tag--completed';
      default:             return '';
    }
  }
}