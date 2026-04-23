<<<<<<< Updated upstream
import { Component } from '@angular/core';
import { Navbar } from '../navbar/navbar';
=======
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Navbar } from '../navbar/navbar';
import { DeleteAccountDto, UpdateProfileDto, UserProfileDto } from '../../dtos/userDto';
import { ItemListDto } from '../../dtos/itemDto';
import { LoanListDto } from '../../dtos/loanDto';
import { ScoreHistoryDto } from '../../dtos/scoreHistoryDto';
import { FineListDto } from '../../dtos/fineDto';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { ItemService } from '../../services/itemService';
import { LoanService } from '../../services/loanService';
import { FineService } from '../../services/fineService';
import { VerificationRequestService } from '../../services/verificationRequestService';
import { PagedRequest, PagedResult } from '../../dtos/paginationDto';
import { ScoreHistoryService } from '../../services/scoreHistoryService';
import { ItemStatus } from '../../dtos/enums';
import { take } from 'rxjs';
>>>>>>> Stashed changes

@Component({
  selector: 'app-user-dashboard',
  imports: [Navbar],
  templateUrl: './user-dashboard.html',
  styleUrl: './user-dashboard.css',
})
<<<<<<< Updated upstream
export class UserDashboard {}
=======
export class UserDashboard implements OnInit {
  // Data
  profile: UserProfileDto | null = null;
  showAvatarModal = false;

  // Stats
  stats: { icon: string; value: string | number; label: string; currency?: string }[] = [];
  totalActiveItems = 0;

  // Tabs
  activeTab: 'items' | 'loans' | 'score' = 'items';
  tabs = [
    { key: 'items' as const, label: 'My Items' },
    { key: 'loans' as const, label: 'Loans' },
    { key: 'score' as const, label: 'Score History' },
  ];

  loanView: 'borrowed' | 'lent' = 'borrowed';

  PAGE_SIZE = 10;

  // --- Items pagination ---
  itemsPage = 1;
  itemsResult: PagedResult<ItemListDto> | null = null;
  itemsLoading = false;

  // --- Borrowed loans pagination ---
  borrowedPage = 1;
  borrowedResult: PagedResult<LoanListDto> | null = null;
  borrowedLoading = false;

  // --- Lent loans pagination ---
  lentPage = 1;
  lentResult: PagedResult<LoanListDto> | null = null;
  lentLoading = false;

  // --- Score history pagination ---
  scorePage = 1;
  scoreResult: PagedResult<ScoreHistoryDto> | null = null;
  scoreLoading = false;

  // Fines (full load for stats only)
  myFines: FineListDto[] = [];

  // Address autocomplete
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;
  private addressSearchTimeout: any;

  // Loading tracking for stats
  private loadedFlags = { profile: false, items: false, loans: false, fines: false };

  // Edit profile
  editMode = false;
  isSaving = false;
  updateSuccess = false;
  updateError = '';
  editForm: UpdateProfileDto = {
    fullName: '',
    userName: '',
    address: '',
    gender: '',
    bio: '',
    avatarUrl: '',
    latitude: undefined,
    longitude: undefined,
  };

  // Change password
  passwordMode = false;
  isSavingPassword = false;
  passwordSuccess = false;
  passwordError = '';
  passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };

  // Delete account
  showDeleteConfirm = false;
  deletePassword = '';
  isDeletingAccount = false;
  deleteError = '';
  showDeleteFinalConfirm = false;

  // Verification modal
  showVerifyModal = false;
  verificationStatus: string | null = null;
  isSubmittingVerify = false;
  verifyError = '';
  verifyForm = { documentUrl: '', documentType: '' };
  documentTypes = [
    { label: '🪪 National ID', value: 'NationalId' },
    { label: '🛂 Passport', value: 'Passport' },
    { label: '🚗 Driving License', value: 'DrivingLicense' },
  ];

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

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private itemService: ItemService,
    private loanService: LoanService,
    private fineService: FineService,
    private scoreHistoryService: ScoreHistoryService,
    private verificationService: VerificationRequestService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      this.activeTab = (params['tab'] as any) || 'items';
      this.loanView = (params['loanView'] as any) || 'borrowed';
      this.itemsPage = +params['itemsPage'] || 1;
      this.borrowedPage = +params['borrowedPage'] || 1;
      this.lentPage = +params['lentPage'] || 1;
      this.scorePage = +params['scorePage'] || 1;

      this.loadProfile();
      this.loadItems(this.itemsPage);
      this.loadBorrowedLoans(this.borrowedPage);
      this.loadLentLoans(this.lentPage);
      this.loadScoreHistory(this.scorePage);
      this.loadFines();
      this.loadVerificationStatus();
    });

    this.loadProfile();
    this.loadItems();
    this.loadBorrowedLoans();
    this.loadLentLoans();
    this.loadScoreHistory();
    this.loadFines();
    this.loadVerificationStatus();
  }

  syncQueryParams() {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        tab: this.activeTab !== 'items' ? this.activeTab : null,
        loanView: this.loanView !== 'borrowed' ? this.loanView : null,
        itemsPage: this.itemsPage > 1 ? this.itemsPage : null,
        borrowedPage: this.borrowedPage > 1 ? this.borrowedPage : null,
        lentPage: this.lentPage > 1 ? this.lentPage : null,
        scorePage: this.scorePage > 1 ? this.scorePage : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  openAvatarModal(): void {
    this.showAvatarModal = true;
  }

  resetPasswordForm() {
    this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
    this.passwordError = '';
    this.passwordSuccess = false;
  }

  // ── Loaders ──────────────────────────────────────────────

  private loadProfile() {
    this.userService.getMyProfile().subscribe({
      next: (res) => {
        if (!res.data) return;
        this.profile = res.data;
        this.editForm = {
          fullName: res.data.fullName,
          userName: res.data.username,
          address: res.data.address,
          gender: res.data.gender ?? '',
          bio: res.data.bio ?? '',
          avatarUrl: res.data.avatarUrl ?? '',
          latitude: res.data.latitude ?? undefined,
          longitude: res.data.longitude ?? undefined,
        };
        this.loadedFlags.profile = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
      },
    });
  }

  private pagedRequest(page: number, size: number): PagedRequest {
    return { page, pageSize: size, sortDescending: true };
  }

  loadItems(page = this.itemsPage) {
    this.itemsLoading = true;
    this.itemsPage = page;
    this.itemService.getMyItems({}, this.pagedRequest(page, this.PAGE_SIZE)).subscribe({
      next: (res) => {
        this.itemsResult = res.data;
        this.itemsLoading = false;
        this.loadedFlags.items = true;

        if (page === 1) {
          this.itemService
            .getMyItems(
              { status: ItemStatus.Approved, isActive: true },
              { page: 1, pageSize: 1, sortDescending: true },
            )
            .subscribe({
              next: (r) => {
                this.totalActiveItems = r.data?.totalCount ?? 0;
                this.checkAndBuildStats();
                this.cdr.detectChanges(); // ← no syncQueryParams here
              },
            });
        }

        this.checkAndBuildStats();
        this.syncQueryParams(); // ← only here
        this.cdr.detectChanges();
      },
      error: () => {
        this.itemsLoading = false;
      },
    });
  }

  loadBorrowedLoans(page = this.borrowedPage) {
    this.borrowedLoading = true;
    this.borrowedPage = page;
    this.loanService.getMyAsBorrower({}, this.pagedRequest(page, 5)).subscribe({
      next: (res) => {
        this.borrowedResult = res.data;
        this.borrowedLoading = false;

        this.loadedFlags.loans = true;
        this.checkAndBuildStats();
        this.syncQueryParams();
        this.cdr.detectChanges();
      },
      error: () => {
        this.borrowedLoading = false;
        this.loadedFlags.loans = true;
        this.checkAndBuildStats();
      },
    });
  }

  loadLentLoans(page = this.lentPage) {
    this.lentLoading = true;
    this.lentPage = page;
    this.cdr.detectChanges();

    this.loanService.getMyAsLender({}, this.pagedRequest(page, 5)).subscribe({
      next: (res) => {
        this.lentResult = res.data;

        // Auto-correct: if the page is empty but items exist, go back to page 1
        if (page > 1 && (res.data?.items?.length ?? 0) === 0 && (res.data?.totalCount ?? 0) > 0) {
          this.loadLentLoans(1);
          return;
        }

        this.lentLoading = false;
        this.syncQueryParams();
        this.cdr.detectChanges();
      },
      error: () => {
        this.lentLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadScoreHistory(page = this.scorePage) {
    this.scoreLoading = true;
    this.scorePage = page;
    this.scoreHistoryService.getMy({}, this.pagedRequest(page, 5)).subscribe({
      next: (res) => {
        this.scoreResult = res.data;
        this.scoreLoading = false;
        this.syncQueryParams();
        this.cdr.detectChanges();
      },
      error: () => {
        this.scoreLoading = false;
      },
    });
  }

  private loadFines() {
    this.fineService.getMyFines({}, { page: 1, pageSize: 200, sortDescending: true }).subscribe({
      next: (res) => {
        this.myFines = res.data?.items ?? [];
        this.loadedFlags.fines = true;
        this.checkAndBuildStats();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadedFlags.fines = true;
        this.checkAndBuildStats();
      },
    });
  }

  private loadVerificationStatus(): void {
    if (this.profile?.isVerified) return;
    this.verificationService
      .getMyRequests({}, { page: 1, pageSize: 1, sortDescending: true })
      .subscribe({
        next: (res) => {
          this.verificationStatus = res.data?.items?.[0]?.status ?? null;
          this.cdr.detectChanges();
        },
        error: () => {
          this.verificationStatus = null;
          this.cdr.detectChanges();
        },
      });
  }

  // ── Stats ─────────────────────────────────────────────────

  private checkAndBuildStats() {
    const { profile, items, loans, fines } = this.loadedFlags;
    if (profile && items && loans && fines) {
      this.buildStats();
      this.cdr.detectChanges();
    }
  }

  private buildStats() {
    const allLoans = this.borrowedResult?.items ?? [];
    const activeLoans = allLoans.filter(
      (l) => l.status === 'Active' || l.status === 'Approved',
    ).length;
    const completedLoans = this.profile?.totalCompletedLoans ?? 0;
    const totalFinesPaid = this.myFines
      .filter((f) => f.status === 'Paid')
      .reduce((sum, f) => sum + f.amount, 0);

    this.stats = [
      { icon: '📦', value: this.totalActiveItems, label: 'Active items' }, // ← uses total, not page slice
      { icon: '🤝', value: activeLoans, label: 'Active loans' },
      { icon: '✅', value: completedLoans, label: 'Completed loans' },
      { icon: '💸', value: totalFinesPaid, currency: 'kr', label: 'Total fines paid' },
    ];
  }

  // ── Pagination helpers ────────────────────────────────────

  get itemPages(): number[] {
    return this.pageRange(this.itemsResult?.totalPages ?? 1);
  }
  get borrowedPages(): number[] {
    return this.pageRange(this.borrowedResult?.totalPages ?? 1);
  }
  get lentPages(): number[] {
    return this.pageRange(this.lentResult?.totalPages ?? 1);
  }
  get scorePages(): number[] {
    return this.pageRange(this.scoreResult?.totalPages ?? 1);
  }

  private pageRange(total: number): number[] {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  // ── Actions ───────────────────────────────────────────────

  submitVerification(): void {
    if (!this.verifyForm.documentUrl || !this.verifyForm.documentType) return;
    this.isSubmittingVerify = true;
    this.verifyError = '';
    this.verificationService
      .submitRequest({
        documentUrl: this.verifyForm.documentUrl,
        documentType: this.verifyForm.documentType as any,
      })
      .subscribe({
        next: () => {
          this.verificationStatus = 'Pending';
          this.isSubmittingVerify = false;
          this.showVerifyModal = false;
          this.verifyForm = { documentUrl: '', documentType: '' };
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.verifyError = err.error?.message ?? 'Something went wrong.';
          this.isSubmittingVerify = false;
        },
      });
  }

  saveProfile() {
    this.isSaving = true;
    this.updateSuccess = false;
    this.updateError = '';
    this.cdr.detectChanges();

    this.userService.updateProfile(this.editForm).subscribe({
      next: (res) => {
        if (res.data) this.profile = res.data as any;
        this.isSaving = false;
        this.updateSuccess = true;
        this.editMode = false; // collapse immediately
        this.cdr.detectChanges(); // force Angular to pick up all three changes at once

        setTimeout(() => {
          this.updateSuccess = false;
          this.cdr.detectChanges();
        }, 2500);
      },
      error: (err) => {
        this.updateError = err.error?.message ?? 'Failed to update profile.';
        this.isSaving = false;
        this.cdr.detectChanges();
      },
    });
  }

  changePassword() {
    if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword) {
      this.passwordError = 'All fields are required.';
      return;
    }
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.passwordError = 'Passwords do not match.';
      return;
    }
    if (this.passwordForm.newPassword.length < 6) {
      this.passwordError = 'Password must be at least 6 characters.';
      return;
    }
    this.isSavingPassword = true;
    this.passwordSuccess = false;
    this.passwordError = '';
    this.authService
      .changePassword({
        currentPassword: this.passwordForm.currentPassword,
        newPassword: this.passwordForm.newPassword,
        confirmNewPassword: this.passwordForm.confirmPassword,
      })
      .subscribe({
        next: () => {
          this.isSavingPassword = false;
          this.passwordSuccess = true;
          this.passwordMode = false;
          this.resetPasswordForm();
          this.cdr.detectChanges();
          setTimeout(() => {
            this.passwordSuccess = false;
            this.cdr.detectChanges();
          }, 2500);
        },
        error: (err) => {
          this.passwordError = err.error?.message ?? 'Failed to update password.';
          this.isSavingPassword = false;
          this.cdr.detectChanges();
        },
      });
  }

  verifyDeletePassword() {
    if (!this.deletePassword) return;
    this.isDeletingAccount = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.userService.deleteAccount({ password: this.deletePassword }).subscribe({
      next: () => {
        // Password was correct and account is deleted — show final confirm before redirect
        this.isDeletingAccount = false;
        this.showDeleteFinalConfirm = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.deleteError = err.error?.message ?? 'Incorrect password.';
        this.isDeletingAccount = false;
        this.showDeleteFinalConfirm = false;
        this.cdr.detectChanges();
      },
    });
  }

  deleteAccount() {
    this.authService.clearTokens();
    this.router.navigate(['/']);
  }

  onAddressInput(value: string) {
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

  selectAddress(place: any) {
    const a = place.address;
    const parts = [
      [a?.road ?? '', a?.house_number ?? ''].filter(Boolean).join(' '),
      a?.neighbourhood ?? a?.suburb ?? '',
      a?.city ?? a?.town ?? a?.village ?? '',
    ].filter(Boolean);
    this.editForm.address = parts.join(', ') || place.display_name;
    this.editForm.latitude = parseFloat(place.lat);
    this.editForm.longitude = parseFloat(place.lon);
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.cdr.detectChanges();
  }

  goToItem(slug: string) {
    this.router.navigate(['/items', slug]);
  }
  goToLoan(id: number) {
    this.router.navigate(['/loans', id]);
  }
  goToBlockedUsers() {
    this.router.navigate(['/blocked-users']);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  getCategoryEmoji(cat: string): string {
    return this.emojiMap[cat.toLowerCase()] ?? '📦';
  }

  getLoanStatusClass(status: string): string {
    // Returns a plain BEM class — colors driven by --pill in user-dashboard.css
    switch (status) {
      case 'Completed':
        return 'status-loan-completed';
      case 'Pending':
      case 'AdminPending':
        return 'status-loan-pending';
      case 'Active':
        return 'status-loan-active';
      case 'Late':
        return 'status-loan-late';
      case 'Rejected':
        return 'status-loan-rejected';
      case 'Cancelled':
        return 'status-loan-cancelled';
      case 'Extended':
        return 'status-loan-extended';
      case 'Approved':
        return 'status-loan-approved';
      default:
        return 'status-loan-default';
    }
  }
}
>>>>>>> Stashed changes
