import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AdminEditUserDto, AdminUserDto } from '../../dtos/adminUserDto';
import { ScoreHistoryDto } from '../../dtos/scoreHistoryDto';
import { AuthService } from '../../services/authService';
import { UserService } from '../../services/userService';
import { UserFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { ScoreChangeReason } from '../../dtos/enums';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import { UserBanHistoryService } from '../../services/userBanHistoryService';
import { ReportService } from '../../services/reportService';
import { SupportService } from '../../services/supportService';
import { FineService } from '../../services/fineService';

type TabKey = 'all' | 'verified' | 'unverified' | 'admin' | 'banned' | 'deleted';

interface SubPage { page: number; pageSize: number; total: number; loading: boolean; }

@Component({
  selector: 'app-admin-user',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './admin-user.html',
  styleUrl: './admin-user.css',
})
export class AdminUser implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private addressTimeout: any;

  // List
  users: AdminUserDto[] = [];
  listLoading = false;
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'all';
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  sortFilter = 'newest';

  // Detail
  selectedId: string | null = null;
  selectedUser: AdminUserDto | null = null;
  userDetail: (AdminUserDto & { scoreHistory?: ScoreHistoryDto[] }) | null = null;
  detailLoading = false;

  // Photo lightbox
  expandedPhoto: string | null = null;

  // Sub-lists
  userLoans: any[] = [];
  userFines: any[] = [];
  userDisputes: any[] = [];
  userAppeals: any[] = [];
  userItems: any[] = [];
  userBanHistory: any[] = [];
  userReports: any[] = [];
  userSupportThreads: any[] = [];
  userScoreHistory: any[] = [];

  itemsPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  loansPage:    SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  finesPage:    SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  disputesPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  appealsPage:  SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  banHistoryPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  reportsPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  supportPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };
  scoreHistoryPage: SubPage = { page: 1, pageSize: 5, total: 0, loading: false };

  itemsCollapsed = true;
  banHistoryCollapsed = true;
  loansCollapsed    = true;
  finesCollapsed    = true;
  disputesCollapsed = true;
  appealsCollapsed  = true;
  scoreHistoryCollapsed = true;
  reportsCollapsed = true;
  supportCollapsed = true;


  // Score adjust
  scoreAdjustment: number | null = null;
  scoreNote = '';
  isAdjustingScore = false;
  scoreError = '';
  scoreSuccess = '';

  // Edit
  showEditForm = false;
  editForm: AdminEditUserDto | null = null;
  isSavingEdit = false;
  editError = '';
  editSuccess = '';

  // Address autocomplete
  addressSuggestions: any[] = [];
  showAddressSuggestions = false;

  // Ban
  showBanForm = false;
  banReason = '';
  banExpiresAt = '';
  isBanning = false;
  banError = '';

  // Unban
  showUnbanConfirm = false;
  unbanReason = '';
  isUnbanning = false;
  unbanError = '';


  // Fine
  showFineForm = false;
  fineAmount: number | null = null;
  fineReason = '';
  issuingFine = false;
  fineError = '';
  fineSuccess = '';

  // Delete
  showDeleteConfirm = false;
  isDeletingUser = false;
  deletionNote = '';

  actionError = '';
  actionSuccess = '';

  tabs = [
    { key: 'all'        as const, label: 'All' },
    { key: 'verified'   as const, label: 'Verified' },
    { key: 'unverified' as const, label: 'Unverified' },
    { key: 'admin'      as const, label: 'Admins' },
    { key: 'banned'     as const, label: 'Banned' },
    { key: 'deleted'    as const, label: 'Deleted' },
  ];

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private banHistoryService: UserBanHistoryService,
    private reportService: ReportService,
    private fineService: FineService,
    private supportService: SupportService,
    public router: Router,
    public cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAdmin()) { this.router.navigate(['/home']); return; }
    this.searchSubject.pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.currentPage = 1; this.loadUsers(); });
    this.loadUsers();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  // ─── TABS ─────────────────────────────────────────────────────────────────

  setTab(tab: TabKey): void { this.activeTab = tab; this.currentPage = 1; this.loadUsers(); }

  // ─── PAGINATION ───────────────────────────────────────────────────────────

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }
  subPages(total: number, pageSize: number): number { return getTotalPages(total, pageSize); }

  // ─── LOAD ─────────────────────────────────────────────────────────────────

  buildFilter(): UserFilter {
    const f: UserFilter = { search: this.searchQuery.trim() || null };
    switch (this.activeTab) {
      // Issue #1: exclude deleted from 'all'
      case 'all':        f.isDeleted = false; break;
      case 'verified':   f.isVerified = true;  f.isDeleted = false; break;
      case 'unverified': f.isVerified = false; f.isDeleted = false; break;
      case 'admin':      f.role = 'Admin';     f.isDeleted = false; break;
      case 'banned':     f.isBanned = true;    f.isDeleted = false; break;
      case 'deleted':    f.isDeleted = true; break;
    }
    return f;
  }

  loadUsers(): void {
    this.listLoading = true;
    this.listError = null;
    const request: PagedRequest = {
      page: this.currentPage, pageSize: this.pageSize,
      sortBy: 'CreatedAt', sortDescending: this.sortFilter !== 'oldest'
    };
    this.userService.getAllIncludingDeleted(this.buildFilter(), request)
      .pipe(takeUntil(this.destroy$), finalize(() => { this.listLoading = false; this.isLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: (res) => { this.users = res.data?.items ?? []; this.totalCount = res.data?.totalCount ?? 0; },
        error: (err) => { this.listError = err.error?.message ?? 'Failed to load users.'; }
      });
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  onFilterChange(): void { this.currentPage = 1; this.loadUsers(); }
  goToPage(p: number): void { if (p < 1 || p > this.totalPages) return; this.currentPage = p; this.loadUsers(); }
  trackById(_: number, u: AdminUserDto): string { return u.id; }

  // ─── DETAIL ───────────────────────────────────────────────────────────────

  openUser(user: AdminUserDto): void {
    if (this.selectedId === user.id) return;
    this.selectedId = user.id;
    this.selectedUser = user;
    this.userDetail = null;
    this.detailLoading = true;
    this.resetForms();
    this.resetSubLists();

    this.userService.getUserById(user.id).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.userDetail = res.data ?? null;
          this.detailLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => { this.actionError = err.error?.message ?? 'Failed to load user.'; this.detailLoading = false; this.cdr.detectChanges(); }
      });
  }

  private resetForms(): void {
    this.showEditForm = false; this.showBanForm = false; this.showUnbanConfirm = false;
    this.showDeleteConfirm = false; this.scoreAdjustment = null; this.scoreNote = '';
    this.actionError = ''; this.actionSuccess = ''; this.editError = ''; this.editSuccess = '';
    this.banError = ''; this.unbanError = ''; this.scoreError = ''; this.scoreSuccess = '';
    this.expandedPhoto = null;
    this.showFineForm = false; this.fineAmount = null; this.fineReason = ''; this.fineError = ''; this.fineSuccess = '';

  }

  private resetSubLists(): void {
    this.userLoans = []; this.userFines = []; this.userDisputes = []; this.userAppeals = [];
    this.userItems = []; this.userBanHistory = []; this.userReports = []; this.userSupportThreads = [];
    this.loansPage    = { page: 1, pageSize: 5, total: 0, loading: false };
    this.finesPage    = { page: 1, pageSize: 5, total: 0, loading: false };
    this.disputesPage = { page: 1, pageSize: 5, total: 0, loading: false };
    this.appealsPage  = { page: 1, pageSize: 5, total: 0, loading: false };
    this.itemsPage    = { page: 1, pageSize: 5, total: 0, loading: false };
    this.banHistoryPage = { page: 1, pageSize: 5, total: 0, loading: false };
    this.reportsPage  = { page: 1, pageSize: 5, total: 0, loading: false };
    this.supportPage  = { page: 1, pageSize: 5, total: 0, loading: false };
    this.scoreHistoryPage = { page: 1, pageSize: 5, total: 0, loading: false };
    this.loansCollapsed = true; this.finesCollapsed = true;
    this.disputesCollapsed = true; this.appealsCollapsed = true;
    this.scoreHistoryCollapsed = true; this.itemsCollapsed = true;
    this.banHistoryCollapsed = true; this.reportsCollapsed = true;
    this.supportCollapsed = true;
  }

  // ─── SUB-LISTS ────────────────────────────────────────────────────────────

  toggleItems(): void {
    this.itemsCollapsed = !this.itemsCollapsed;
    if (!this.itemsCollapsed && this.userItems.length === 0) this.loadItems();
  }

  toggleBanHistory(): void {
    this.banHistoryCollapsed = !this.banHistoryCollapsed;
    if (!this.banHistoryCollapsed && this.userBanHistory.length === 0) this.loadBanHistory();
  }

  toggleReports(): void {
    this.reportsCollapsed = !this.reportsCollapsed;
    if (!this.reportsCollapsed && this.userReports.length === 0) this.loadReports();
  }

  toggleSupport(): void {
    this.supportCollapsed = !this.supportCollapsed;
    if (!this.supportCollapsed && this.userSupportThreads.length === 0) this.loadSupport();
  }

  toggleLoans(): void {
    this.loansCollapsed = !this.loansCollapsed;
    if (!this.loansCollapsed && this.userLoans.length === 0) this.loadLoans();
  }

  toggleFines(): void {
    this.finesCollapsed = !this.finesCollapsed;
    if (!this.finesCollapsed && this.userFines.length === 0) this.loadFines();
  }

  toggleDisputes(): void {
    this.disputesCollapsed = !this.disputesCollapsed;
    if (!this.disputesCollapsed && this.userDisputes.length === 0) this.loadDisputes();
  }

  toggleAppeals(): void {
    this.appealsCollapsed = !this.appealsCollapsed;
    if (!this.appealsCollapsed && this.userAppeals.length === 0) this.loadAppeals();
  }

  toggleScoreHistory(): void {
    this.scoreHistoryCollapsed = !this.scoreHistoryCollapsed;
    if (!this.scoreHistoryCollapsed && !this.userDetail?.scoreHistory) this.loadScoreHistory(this.selectedId!);
  }

  loadLoans(page = 1): void {
    if (!this.selectedId) return;
    this.loansPage.loading = true; this.loansPage.page = page;
    this.userService.getUserLoans(this.selectedId, {}, { page, pageSize: this.loansPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { 
        
        this.loansPage.loading = false; 
        console.log(this.loansPage)
        
        
        this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userLoans = res.data?.items ?? []; this.loansPage.total = res.data?.totalCount ?? 0; } });
  }

  loadFines(page = 1): void {
    if (!this.selectedId) return;
    this.finesPage.loading = true; this.finesPage.page = page;
    this.userService.getUserFines(this.selectedId, {}, { page, pageSize: this.finesPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.finesPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userFines = res.data?.items ?? []; this.finesPage.total = res.data?.totalCount ?? 0; } });
  }

  loadDisputes(page = 1): void {
    if (!this.selectedId) return;
    this.disputesPage.loading = true; this.disputesPage.page = page;
    this.userService.getUserDisputes(this.selectedId, {}, { page, pageSize: this.disputesPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.disputesPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userDisputes = res.data?.items ?? []; this.disputesPage.total = res.data?.totalCount ?? 0; } });
  }

  loadAppeals(page = 1): void {
    if (!this.selectedId) return;
    this.appealsPage.loading = true; this.appealsPage.page = page;
    this.userService.getUserAppeals(this.selectedId, {}, { page, pageSize: this.appealsPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.appealsPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userAppeals = res.data?.items ?? []; this.appealsPage.total = res.data?.totalCount ?? 0; } });
  }

  loadItems(page = 1): void {
    if (!this.selectedId) return;
    this.itemsPage.loading = true; this.itemsPage.page = page;
    this.userService.getUserItems(this.selectedId, {}, { page, pageSize: this.itemsPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.itemsPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userItems = res.data?.items ?? []; this.itemsPage.total = res.data?.totalCount ?? 0; } });
  }

  loadBanHistory(page = 1): void {
    if (!this.selectedId) return;
    this.banHistoryPage.loading = true; this.banHistoryPage.page = page;
    this.banHistoryService.getByUserId(this.selectedId, {}, { page, pageSize: this.banHistoryPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.banHistoryPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userBanHistory = res.data?.items ?? []; this.banHistoryPage.total = res.data?.totalCount ?? 0; } });
  }

  loadReports(page = 1): void {
    if (!this.selectedId) return;
    this.reportsPage.loading = true; this.reportsPage.page = page;
    this.userService.getUserReports(this.selectedId, { page, pageSize: this.reportsPage.pageSize, sortDescending: true })
      .pipe(takeUntil(this.destroy$), finalize(() => { this.reportsPage.loading = false; this.cdr.detectChanges(); }))
      .subscribe({ next: (res) => { this.userReports = res.data?.items ?? []; this.reportsPage.total = res.data?.totalCount ?? 0; } });
  }

  loadSupport(page = 1): void {
    if (!this.selectedId) return;
    this.supportPage.loading = true; this.supportPage.page = page;
    this.supportService.adminGetAll(
      { userId: this.selectedId },
      { page, pageSize: this.supportPage.pageSize, sortDescending: true }
    ).pipe(takeUntil(this.destroy$), finalize(() => { this.supportPage.loading = false; this.cdr.detectChanges(); }))
    .subscribe({ next: (res) => { this.userSupportThreads = res.data?.items ?? []; this.supportPage.total = res.data?.totalCount ?? 0; } });
  }

  get pagedScoreHistory(): ScoreHistoryDto[] {
    if (!this.userDetail?.scoreHistory) return [];
    const start = (this.scoreHistoryPage.page - 1) * this.scoreHistoryPage.pageSize;
    return this.userDetail.scoreHistory.slice(start, start + this.scoreHistoryPage.pageSize);
  }


  private loadScoreHistory(userId: string): void {
  this.userService.getUserScoreHistory(userId, {}, { page: 1, pageSize: 100, sortDescending: true })
    .pipe(takeUntil(this.destroy$))
    .subscribe(res => {
      if (this.userDetail && res.data) {
        this.userDetail.scoreHistory = res.data.items;
        this.scoreHistoryPage.total = res.data.items.length;
        this.scoreHistoryPage.page = 1;
        this.cdr.detectChanges();
      }
    });
}
  // ─── ADDRESS AUTOCOMPLETE ─────────────────────────────────────────────────

  onAddressInput(value: string): void {
    clearTimeout(this.addressTimeout);
    this.showAddressSuggestions = false;
    if (!value || value.length < 3) { this.addressSuggestions = []; return; }
    this.addressTimeout = setTimeout(() => {
      const apiKey = '6efe16ed3bb047b8975d6f4738a471a9';
      fetch(`https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(value)}&limit=5&apiKey=${apiKey}`)
        .then(r => r.json())
        .then(data => { this.addressSuggestions = data.features ?? []; this.showAddressSuggestions = true; this.cdr.detectChanges(); });
    }, 400);
  }

  selectAddressSuggestion(place: any): void {
    if (!this.editForm) return;
    this.editForm.address = place.properties.formatted;
    this.editForm.latitude = place.geometry.coordinates[1];
    this.editForm.longitude = place.geometry.coordinates[0];
    this.addressSuggestions = [];
    this.showAddressSuggestions = false;
    this.cdr.detectChanges();
  }

  // ─── EDIT ─────────────────────────────────────────────────────────────────

  openEdit(): void {
    if (!this.userDetail) return;
    // Issue #3: removed isVerified from edit form
    this.editForm = {
      fullName:   this.userDetail.fullName,
      username:   this.userDetail.username,
      email:      this.userDetail.email,
      avatarUrl:  this.userDetail.avatarUrl,
      gender:     this.userDetail.gender,
      address:    this.userDetail.address,
      role:       this.userDetail.role,
      score:      this.userDetail.score,
    };
    this.showEditForm = true; this.editError = ''; this.editSuccess = '';
  }

  saveEdit(): void {
    if (!this.userDetail || !this.editForm) return;

    const dto = { ...this.editForm };
      if (!dto.newPassword?.trim()) {
        dto.newPassword = null;
    }
  

    this.isSavingEdit = true; 
    this.userService.updateUser(this.userDetail.id, dto)
      .pipe(finalize(() => { this.isSavingEdit = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: (res) => {
          if (res.data) {
            this.userDetail = { ...this.userDetail, ...res.data };
            this.editSuccess = '✓ User updated successfully.';
            this.showEditForm = false;
            const idx = this.users.findIndex(u => u.id === res.data!.id);
            if (idx !== -1) this.users[idx] = { ...this.users[idx], ...res.data };
          }
          setTimeout(() => this.editSuccess = '', 3000);
        },
        error: (err) => { this.editError = err.error?.message ?? 'Failed to update user.'; }
      });
  }

  // ─── VERIFY TOGGLE ────────────────────────────────────────────────────────

  toggleVerified(): void {
    if (!this.userDetail) return;
    this.userService.updateUser(this.userDetail.id, { isVerified: !this.userDetail.isVerified })
      .subscribe({
        next: (res) => {
          if (res.data) {
            this.userDetail!.isVerified = res.data.isVerified;
            this.actionSuccess = res.data.isVerified ? '✓ User verified.' : '✓ User unverified.';
            const idx = this.users.findIndex(u => u.id === res.data!.id);
            if (idx !== -1) this.users[idx].isVerified = res.data.isVerified;
          }
          this.cdr.detectChanges();
          setTimeout(() => this.actionSuccess = '', 3000);
        },
        error: (err) => { this.actionError = err.error?.message ?? 'Failed to toggle verification.'; this.cdr.detectChanges(); }
      });
  }

  // ─── SCORE ────────────────────────────────────────────────────────────────

  adjustScore(): void {
    if (!this.userDetail || this.scoreAdjustment === null) return;
    this.isAdjustingScore = true; this.scoreError = '';
    console.log(this.userDetail)
    this.userService.adjustScore(this.userDetail.id, {
      pointsChanged: this.scoreAdjustment,
      reason: ScoreChangeReason.AdminAdjustment,
      note: this.scoreNote.trim() || 'Manual adjustment',
      userId: this.userDetail.id 
    }).subscribe({
      next: () => {
        
        this.scoreSuccess = '✓ Score adjusted.';
        this.scoreAdjustment = null; this.scoreNote = ''; this.isAdjustingScore = false;
        // Reload detail + score history
        this.userService.getUserById(this.userDetail!.id).subscribe(res => {
          if (res.data) {
            const prev = this.userDetail;
            this.userDetail = { ...res.data, scoreHistory: prev?.scoreHistory };
            const idx = this.users.findIndex(u => u.id === res.data!.id);
            if (idx !== -1) this.users[idx].score = res.data.score;
            this.cdr.detectChanges();
          }
        });
        if (!this.scoreHistoryCollapsed) this.loadScoreHistory(this.userDetail!.id);
        setTimeout(() => this.scoreSuccess = '', 3000);
        this.cdr.detectChanges();
      },
      error: (err) => { this.scoreError = err.error?.message ?? 'Failed to adjust score.'; this.isAdjustingScore = false; this.cdr.detectChanges(); }
    });
  }

  // ─── BAN ──────────────────────────────────────────────────────────────────

  openBanForm(): void { this.showBanForm = true; this.banReason = ''; this.banExpiresAt = ''; this.banError = ''; }

  banUser(): void {
    if (!this.userDetail || !this.banReason.trim()) { this.banError = 'Reason is required.'; return; }
    this.isBanning = true; this.banError = '';
    const banExpiresAtUtc = this.banExpiresAt ? new Date(this.banExpiresAt).toISOString() : null;
      this.userService.banUser(this.userDetail.id, { reason: this.banReason.trim(), banExpiresAt: banExpiresAtUtc })
        .subscribe({
        next: () => {
          // Reload detail to get fresh ban info from server
          this.userService.getUserById(this.userDetail!.id).subscribe(res => {
            if (res.data) {
              this.userDetail = { ...res.data, scoreHistory: this.userDetail?.scoreHistory };
              const idx = this.users.findIndex(u => u.id === res.data!.id);
              if (idx !== -1) this.users[idx].isBanned = true;
              this.cdr.detectChanges();
            }
          });
          this.isBanning = false; this.showBanForm = false;
          this.actionSuccess = '✓ User banned.';
          setTimeout(() => this.actionSuccess = '', 3000);
          this.cdr.detectChanges();
        },
        error: (err) => { this.banError = err.error?.message ?? 'Failed to ban user.'; this.isBanning = false; this.cdr.detectChanges(); }
      });
  }

  // ─── UNBAN ────────────────────────────────────────────────────────────────

  openUnbanConfirm(): void { this.showUnbanConfirm = true; this.unbanReason = ''; this.unbanError = ''; }

  unbanUser(): void {
    if (!this.userDetail) return;
    this.isUnbanning = true; this.unbanError = '';
    this.userService.unbanUser(this.userDetail.id, { reason: this.unbanReason.trim() || 'Unbanned by admin' })
      .subscribe({
        next: () => {
          this.userDetail!.isBanned = false;
          this.userDetail!.banReason = null; this.userDetail!.banExpiresAt = null; this.userDetail!.bannedAt = null;
          this.isUnbanning = false; this.showUnbanConfirm = false;
          this.actionSuccess = '✓ User unbanned.';
          const idx = this.users.findIndex(u => u.id === this.userDetail!.id);
          if (idx !== -1) this.users[idx].isBanned = false;
          this.cdr.detectChanges();
          setTimeout(() => this.actionSuccess = '', 3000);
        },
        error: (err) => { this.unbanError = err.error?.message ?? 'Failed to unban user.'; this.isUnbanning = false; this.cdr.detectChanges(); }
      });
  }

  // ─── DELETE ───────────────────────────────────────────────────────────────

  deleteUser(): void {
    if (!this.userDetail) return;
    this.isDeletingUser = true;
    this.userService.deleteUser(this.userDetail.id, this.deletionNote || undefined)
      .subscribe({
        next: () => {
          this.showDeleteConfirm = false; this.selectedId = null; this.userDetail = null;
          const idx = this.users.findIndex(u => u.id === this.selectedUser?.id);
          if (idx !== -1) this.users[idx].isDeleted = true;
          this.isDeletingUser = false;
          this.cdr.detectChanges();
        },
        error: (err) => { this.actionError = err.error?.message ?? 'Failed to delete user.'; this.isDeletingUser = false; this.cdr.detectChanges(); }
      });
  }


  //Fine
  openFineForm(): void { this.showFineForm = true; this.fineAmount = null; this.fineReason = ''; this.fineError = ''; this.fineSuccess = ''; }

  issueFine(): void {
    if (!this.userDetail || !this.fineAmount || !this.fineReason.trim()) { this.fineError = 'Amount and reason are required.'; return; }
    this.issuingFine = true; this.fineError = '';
    this.fineService.adminIssueCustomFine({ userId: this.userDetail.id, amount: this.fineAmount, reason: this.fineReason.trim() })
      .pipe(finalize(() => { this.issuingFine = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.showFineForm = false;
          this.actionSuccess = '✓ Fine issued.';
          this.userService.getUserById(this.userDetail!.id).subscribe(res => {
            if (res.data) {
              this.userDetail = { ...res.data, scoreHistory: this.userDetail?.scoreHistory };
              const idx = this.users.findIndex(u => u.id === res.data!.id);
              if (idx !== -1) this.users[idx] = { ...this.users[idx], ...res.data };
              this.cdr.detectChanges();
            }
          });
          if (!this.finesCollapsed) this.loadFines();
          setTimeout(() => this.actionSuccess = '', 3000);
        },
        error: (err) => { this.fineError = err.error?.message ?? 'Failed to issue fine.'; }
      });
  }

  // ─── UI HELPERS ───────────────────────────────────────────────────────────

  getInitials(name: string): string { return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? ''; }

  getScoreClass(score: number): string {
    if (score >= 70) return 'text-emerald-400';
    if (score >= 40) return 'text-amber-400';
    return 'text-red-400';
  }

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':    return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved':  return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'completed': return 'bg-zinc-700 text-zinc-300 border-zinc-600';
      case 'late':      return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'rejected':  return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'cancelled': return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      case 'pending':   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending': return 'bg-indigo-400/10 text-indigo-400 border-indigo-400/20';
      case 'extended':  return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getDisputeStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'awaitingresponse':    return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'pendingadminreview':  return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'resolved':            return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'pastdeadline':        return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'cancelled':           return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:                    return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getFineStatusClass(status: string): string {
    switch (status) {
      case 'Unpaid':              return 'text-red-400';
      case 'PendingVerification': return 'text-amber-400';
      case 'Paid':                return 'text-emerald-400';
      default:                    return 'text-zinc-400';
    }
  }

  getAppealStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending':  return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'approved': return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'rejected': return 'bg-red-400/10 text-red-400 border-red-400/20';
      default:         return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getItemStatusClass(status: string): string {
    switch (status) {
      case 'Approved':  return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'Pending':   return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'Rejected':  return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'Deleted':   return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:          return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getReportStatusClass(status: string): string {
    switch (status) {
      case 'Pending':     return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'UnderReview': return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'Resolved':    return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'Dismissed':   return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:            return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getSupportStatusClass(status: string): string {
    switch (status) {
      case 'Open':    return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'Claimed': return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'Closed':  return 'bg-zinc-700 text-zinc-400 border-zinc-600';
      default:        return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }
}