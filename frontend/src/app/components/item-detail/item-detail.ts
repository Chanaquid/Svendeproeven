import { Component } from '@angular/core';

@Component({
  selector: 'app-item-detail',
  imports: [],
  templateUrl: './item-detail.html',
  styleUrl: './item-detail.css',
})
<<<<<<< Updated upstream
export class ItemDetail {}
=======
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
    pickupAddress: '',
    pickupLatitude: 0 as number,
    pickupLongitude: 0 as number,
    requiresVerification: false,
    isActive: true,
    photoUrl: '',
  };
  isSavingEdit = false;
  editError = '';
  editSuccess = '';
  descriptionExpanded = false;

  // Address autocomplete
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

  // Reviews
  visibleReviews = 5;
  get displayedReviews() {
    return this.reviews.slice(0, this.visibleReviews);
  }
  loadMoreReviews() {
    this.visibleReviews += 5;
  }

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return (
      Math.round((this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length) * 10) / 10
    );
  }

  get isOwner(): boolean {
    return !!this.item && this.item.ownerId === this.currentUserId;
  }

  get canRequestLoan(): boolean {
    if (!this.item) return false;
    return (
      this.item.status === 'Approved' &&
      this.item.isActive &&
      !this.item.isCurrentlyOnLoan &&
      !this.isOwner &&
      !this.existingLoan &&
      (!this.item.requiresVerification || this.isVerified)
    );
  }

  get hasActiveLoanRequest(): boolean {
    if (!this.existingLoan) return false;
    return ['Pending', 'AdminPending', 'Approved', 'Active', 'Late'].includes(
      this.existingLoan.status,
    );
  }

  get displayedDisputes() {
    return this.disputeHistory.slice(0, this.visibleDisputes);
  }
  get displayedLoanHistory() {
    return this.loanHistory.slice(0, this.visibleLoans);
  }
  loadMoreDisputes() {
    this.visibleDisputes += 5;
  }
  loadMoreLoanHistory() {
    this.visibleLoans += 5;
  }

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
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();

    if (this.authService.isLoggedIn()) {
      this.userService.getMyProfile().subscribe({
        next: (res) => {
          this.currentUserId = res.data?.id ?? '';
          this.isVerified = res.data?.isVerified ?? false;
          this.cdr.detectChanges();
        },
        error: () => {},
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
        }

        if (this.isOwner || this.isAdmin) {
          this.loadLoanHistory(this.item.id);
          this.loadDisputeHistory(this.item.id);
        }
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private checkExistingLoan(): void {
    if (!this.item) return;
    this.loanService.getMyLoanForItem(this.item.id).subscribe({
      next: (res) => {
        if (res.data) {
          this.existingLoan = res.data;
        }
        this.cdr.detectChanges();
      },
      error: () => {},
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
      },
    });
  }

  private loadLoanHistory(itemId: number): void {
    this.isLoadingHistory = true;
    const filter = { itemId };
    const request: PagedRequest = { page: 1, pageSize: 50 };
    this.loanService.getMyAsLender(filter, request).subscribe({
      next: (res) => {
        this.loanHistory = (res.data?.items ?? []).filter((l) => l.itemId === itemId);
        this.isLoadingHistory = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingHistory = false;
        this.cdr.detectChanges();
      },
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
        // fallback for non-admin — use getMyAll filtered
        this.disputeService.getMyAll(filter, request).subscribe({
          next: (res2) => {
            this.disputeHistory = (res2.data?.items ?? []).filter((d: any) => d.itemId === itemId);
            this.isLoadingDisputes = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoadingDisputes = false;
            this.cdr.detectChanges();
          },
        });
      },
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

    this.loanService
      .create({
        itemId: this.item.id,
        startDate: this.loanForm.startDate,
        endDate: this.loanForm.endDate,
      })
      .subscribe({
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
        },
      });
  }

  cancelLoan(): void {
    if (!this.existingLoan) return;
    this.isCancellingLoan = true;
    this.cancelError = '';

    this.loanService
      .cancel(this.existingLoan.id, {
        loanId: this.existingLoan.id,
        reason: '',
      })
      .subscribe({
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
        },
      });
  }

  openLoanModal(): void {
    if (!this.item) return;

    const availableFrom = new Date(this.item.availableFrom);
    const today = new Date();
    today.setHours(12, 0, 0, 0);

    // Use the later of today or availableFrom as the start date
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
      pickupAddress: this.item.pickupAddress ?? '',
      pickupLatitude: this.item.pickupLatitude ?? 0,
      pickupLongitude: this.item.pickupLongitude ?? 0,
      requiresVerification: this.item.requiresVerification ?? false,
      isActive: this.item.isActive ?? true,
      photoUrl: this.item.photos?.[0]?.photoUrl ?? '',
    };
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

  saveEdit(): void {
    if (!this.item) return;
    if (!this.editForm.title?.trim()) {
      this.editError = 'Title is required.';
      return;
    }

    this.isSavingEdit = true;
    this.editError = '';
    this.editSuccess = '';

    this.itemService
      .update(this.item.id, {
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
      })
      .subscribe({
        next: (res) => {
          if (res.data) {
            this.item = res.data;
            this.updatePhoto(res.data);
          }
        },
        error: (err) => {
          this.editError = err.error?.message ?? 'Failed to save changes.';
          this.isSavingEdit = false;
          this.cdr.detectChanges();
        },
      });
  }

  private updatePhoto(updated: ItemDto): void {
    const newUrl = this.editForm.photoUrl?.trim();
    const existingPhoto = updated.photos?.[0];

    const finish = (finalItem?: ItemDto) => {
      this.item = finalItem ?? updated;
      this.isSavingEdit = false;
      this.editSuccess = '✓ Changes saved!';
      this.cdr.detectChanges();

      setTimeout(() => {
        this.showEditModal = false;
        this.editSuccess = '';
        if (finalItem?.slug) {
          this.router.navigate(['/items', finalItem.slug], {
            replaceUrl: true,
          });
        }

        this.cdr.detectChanges();
      }, 1200);
    };

    const addNew = () => {
      if (!newUrl) {
        finish(updated);
        return;
      }
      this.itemService
        .addPhoto(updated.id, { photoUrl: newUrl, isPrimary: true, displayOrder: 0 })
        .subscribe({
          next: () =>
            this.itemService.getById(updated.id).subscribe({
              next: (res) => finish(res.data ?? updated),
              error: () => finish(updated),
            }),
          error: () => finish(updated),
        });
    };

    if (existingPhoto && newUrl !== existingPhoto.photoUrl) {
      this.itemService.deletePhoto(updated.id, existingPhoto.id).subscribe({
        next: addNew,
        error: addNew,
      });
    } else if (!existingPhoto && newUrl) {
      addNew();
    } else {
      finish(updated);
    }
  }

  // ── Address autocomplete ──────────────────────────────────────

  onAddressInput(value: string): void {
    clearTimeout(this.addressSearchTimeout);
    this.showAddressSuggestions = false;
    if (!value || value.length < 3) {
      this.addressSuggestions = [];
      return;
    }
    this.addressSearchTimeout = setTimeout(() => {
      fetch(
        `https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&q=${encodeURIComponent(value)}&limit=5`,
      )
        .then((res) => res.json())
        .then((data) => {
          this.addressSuggestions = data;
          this.showAddressSuggestions = true;
          this.cdr.detectChanges();
        });
    }, 400);
  }

  selectAddress(suggestion: any): void {
    this.editForm.pickupAddress = suggestion.display_name;
    this.editForm.pickupLatitude = parseFloat(suggestion.lat);
    this.editForm.pickupLongitude = parseFloat(suggestion.lon);
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
    if (!this.reportReason) {
      this.reportError = 'Please select a reason.';
      return;
    }
    this.isSubmittingReport = true;
    this.reportError = '';

    this.reportService
      .create({
        type: ReportType.Item,
        targetId: this.item!.id.toString(),
        reasons: this.reportReason as ReportReason,
        additionalDetails: this.reportDetails.trim() || null,
      })
      .subscribe({
        next: () => {
          this.isSubmittingReport = false;
          this.reportSuccess = '✓ Report submitted. Thank you.';
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

  get totalLoanPrice(): number | null {
    if (!this.item || this.item.isFree) return null;
    if (!this.loanForm.startDate || !this.loanForm.endDate) return null;
    const start = new Date(this.loanForm.startDate);
    const end = new Date(this.loanForm.endDate);
    const days = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    if (days <= 0) return null;
    return days * this.item.pricePerDay;
  }

  get loanDays(): number {
    if (!this.loanForm.startDate || !this.loanForm.endDate) return 0;
    const start = new Date(this.loanForm.startDate);
    const end = new Date(this.loanForm.endDate);
    return Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
  }

  // ── Helpers ───────────────────────────────────────────────────

  private toDateInputValue(value: string | Date | null | undefined): string {
    if (!value) return '';
    if (typeof value === 'string') return value.slice(0, 10);

    const y = value.getFullYear();
    const m = String(value.getMonth() + 1).padStart(2, '0');
    const d = String(value.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  getCategoryEmoji(cat: string): string {
    return getCategoryEmoji(cat);
  }

  // Returns plain class names (e.g. 'cond-excellent') used by .condition-badge
  getConditionClass(condition: string): string {
    return getConditionClass(condition as ItemCondition);
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

  goBack(): void {
    window.history.back();
  }

  // Returns plain class names (e.g. 'status-loan-active') used by .history-status-tag
  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'status-loan-active';
      case 'approved':
        return 'status-loan-approved';
      case 'late':
        return 'status-loan-late';
      case 'pending':
        return 'status-loan-pending';
      case 'adminpending':
        return 'status-loan-adminpending';
      case 'cancelled':
        return 'status-loan-cancelled';
      case 'rejected':
        return 'status-loan-rejected';
      case 'completed':
        return 'status-loan-completed';
      default:
        return '';
    }
  }
}
>>>>>>> Stashed changes
