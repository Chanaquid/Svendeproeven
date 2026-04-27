import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { DisputeService } from '../../services/disputeService';
import {
  AdminResolveDisputeDto,
  DisputeDto,
  DisputeListDto,
  DisputePenaltyDto,
} from '../../dtos/disputeDto';
import { DisputeFilter } from '../../dtos/filterDto';
import { DisputeFiledAs, DisputeStatus, DisputeVerdict } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'awaiting' | 'pending' | 'overdue' | 'resolved' | 'cancelled';

@Component({
  selector: 'app-admin-dispute',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './admin-dispute.html',
  styleUrl: './admin-dispute.css',
})
export class AdminDispute implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadDisputes(); };

  disputes: DisputeListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pending';

  currentPage = 1;
  totalCount = 0;

  showModal = false;
  isLoadingDetail = false;
  selectedItem: DisputeListDto | null = null;
  detail: DisputeDto | null = null;
  selectedPhoto: string | null = null;
  selectedPhotoCaption: string | null = null;

  resolveVerdict: DisputeVerdict | '' = '';
  resolveAdminNote = '';
  ownerFine: number | null = null;
  ownerScore: number | null = null;
  borrowerFine: number | null = null;
  borrowerScore: number | null = null;

  resolveError = '';
  resolveSuccess = '';
  isResolving = false;
  showResolveForm = false;

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',      label: 'Alle' },
    { key: 'awaiting', label: 'Afventer svar' },
    { key: 'pending',  label: 'Under gennemgang' },
    { key: 'overdue',  label: 'Overskredet' },
    { key: 'resolved', label: 'Afgjort' },
    { key: 'cancelled',label: 'Annulleret' },
  ];

  readonly DisputeStatus = DisputeStatus;
  readonly DisputeVerdict = DisputeVerdict;
  readonly DisputeFiledAs = DisputeFiledAs;

  verdictOptions = [
    { value: DisputeVerdict.NoPenalty,         label: 'Ingen straf',          desc: 'Luk uden straf til nogen part' },
    { value: DisputeVerdict.OwnerPenalized,    label: 'Ejer straffes',        desc: 'Ejeren bærer ansvaret' },
    { value: DisputeVerdict.BorrowerPenalized, label: 'Låner straffes',       desc: 'Låneren bærer ansvaret' },
    { value: DisputeVerdict.BothPenalized,     label: 'Begge straffes',       desc: 'Begge parter deler ansvaret' },
  ];

  constructor(
    private authService: AuthService,
    private disputeService: DisputeService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 96));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadDisputes();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadDisputes();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadDisputes(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, DisputeStatus>> = {
      awaiting: DisputeStatus.AwaitingResponse,
      pending:  DisputeStatus.PendingAdminReview,
      overdue:  DisputeStatus.PastDeadline,
      resolved: DisputeStatus.Resolved,
      cancelled:DisputeStatus.Cancelled,
    };

    const filter: DisputeFilter = {
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.disputeService
      .adminGetAll(filter, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.disputes = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente tvister. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.disputeService
      .adminGetAll(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const tab = this.tabs.find((t) => t.key === 'all');
          if (tab) {
            tab.count = res.data?.totalCount ?? 0;
            this.cdr.detectChanges();
          }
        },
      });

    const statusTabs: { key: TabKey; status: DisputeStatus }[] = [
      { key: 'awaiting', status: DisputeStatus.AwaitingResponse },
      { key: 'pending',  status: DisputeStatus.PendingAdminReview },
      { key: 'overdue',  status: DisputeStatus.PastDeadline },
      { key: 'resolved', status: DisputeStatus.Resolved },
      { key: 'cancelled',status: DisputeStatus.Cancelled },
    ];

    for (const { key, status } of statusTabs) {
      this.disputeService
        .adminGetAll({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            const tab = this.tabs.find((t) => t.key === key);
            if (tab) {
              tab.count = res.data?.totalCount ?? 0;
              this.cdr.detectChanges();
            }
          },
        });
    }
  }

  switchTab(key: TabKey): void {
    this.activeTab = key;
    this.currentPage = 1;
    this.loadDisputes();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadDisputes();
  }

  openModal(item: DisputeListDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.resetResolveForm();
    this.selectedPhoto = null;

    this.disputeService
      .adminGetById(item.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoadingDetail = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => { this.detail = res.data ?? null; },
        error: () => { this.showModal = false; },
      });
  }

  closeModal(): void {
    this.showModal = false;
    this.detail = null;
    this.selectedPhoto = null;
  }

  get canResolve(): boolean {
    return (
      this.detail?.status === DisputeStatus.PendingAdminReview ||
      this.detail?.status === DisputeStatus.PastDeadline ||
      this.detail?.status === DisputeStatus.AwaitingResponse
    );
  }

  get showOwnerPenalty(): boolean {
    return (
      this.resolveVerdict === DisputeVerdict.OwnerPenalized ||
      this.resolveVerdict === DisputeVerdict.BothPenalized
    );
  }

  get showBorrowerPenalty(): boolean {
    return (
      this.resolveVerdict === DisputeVerdict.BorrowerPenalized ||
      this.resolveVerdict === DisputeVerdict.BothPenalized
    );
  }

  private resetResolveForm(): void {
    this.showResolveForm = false;
    this.resolveVerdict = '';
    this.resolveAdminNote = '';
    this.ownerFine = null;
    this.ownerScore = null;
    this.borrowerFine = null;
    this.borrowerScore = null;
    this.resolveError = '';
    this.resolveSuccess = '';
    this.isResolving = false;
  }

  resolve(): void {
    if (!this.detail || !this.resolveVerdict) return;
    this.isResolving = true;
    this.resolveError = '';

    const ownerPenalty: DisputePenaltyDto | null = this.showOwnerPenalty
      ? { fineAmount: this.ownerFine ?? undefined, scoreAdjustment: this.ownerScore ?? undefined }
      : null;

    const borrowerPenalty: DisputePenaltyDto | null = this.showBorrowerPenalty
      ? { fineAmount: this.borrowerFine ?? undefined, scoreAdjustment: this.borrowerScore ?? undefined }
      : null;

    const dto: AdminResolveDisputeDto = {
      verdict: this.resolveVerdict as DisputeVerdict,
      adminNote: this.resolveAdminNote.trim() || undefined,
      ownerPenalty,
      borrowerPenalty,
    };

    this.disputeService
      .adminResolve(this.detail.id, dto)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isResolving = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.resolveSuccess = 'Tvist afgjort.';
          this.showResolveForm = false;
          this.loadDisputes();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.resolveSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.resolveError = err.error?.message ?? 'Kunne ikke afgøre tvist.';
        },
      });
  }

  openPhoto(url: string, caption?: string | null): void {
    this.selectedPhoto = url;
    this.selectedPhotoCaption = caption ?? null;
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case DisputeStatus.AwaitingResponse:   return 'badge badge-warning';
      case DisputeStatus.PendingAdminReview: return 'badge badge-info';
      case DisputeStatus.Resolved:           return 'badge badge-success';
      case DisputeStatus.PastDeadline:       return 'badge badge-danger';
      case DisputeStatus.Cancelled:          return 'badge';
      default:                               return 'badge';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case DisputeStatus.AwaitingResponse:   return 'Afventer svar';
      case DisputeStatus.PendingAdminReview: return 'Under gennemgang';
      case DisputeStatus.Resolved:           return 'Afgjort';
      case DisputeStatus.PastDeadline:       return 'Overskredet';
      case DisputeStatus.Cancelled:          return 'Annulleret';
      default:                               return status;
    }
  }

  getVerdictBadge(verdict: string): string {
    switch (verdict) {
      case DisputeVerdict.NoPenalty:         return 'badge badge-success';
      case DisputeVerdict.OwnerPenalized:    return 'badge badge-warning';
      case DisputeVerdict.BorrowerPenalized: return 'badge badge-danger';
      case DisputeVerdict.BothPenalized:     return 'badge badge-warning';
      default:                               return 'badge';
    }
  }

  getVerdictLabel(verdict: string): string {
    switch (verdict) {
      case DisputeVerdict.NoPenalty:         return 'Ingen straf';
      case DisputeVerdict.OwnerPenalized:    return 'Ejer straffet';
      case DisputeVerdict.BorrowerPenalized: return 'Låner straffet';
      case DisputeVerdict.BothPenalized:     return 'Begge straffet';
      default:                               return verdict;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
