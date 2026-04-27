import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { FineService } from '../../services/fineService';
import { FineDto, FineListDto } from '../../dtos/fineDto';
import { FineFilter } from '../../dtos/filterDto';
import { FineStatus, FineType, PaymentMethod } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

type TabKey = 'all' | 'unpaid' | 'pendingVerification' | 'paid' | 'rejected' | 'voided';

@Component({
  selector: 'app-admin-fine',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './admin-fine.html',
  styleUrl: './admin-fine.css',
})
export class AdminFine implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadFines(); };

  fines: FineListDto[] = [];
  isLoading = true;
  listError: string | null = null;
  searchQuery = '';
  activeTab: TabKey = 'pendingVerification';

  currentPage = 1;
  totalCount = 0;

  showModal = false;
  isLoadingDetail = false;
  selectedItem: FineListDto | null = null;
  detail: FineDto | null = null;
  selectedPhoto: string | null = null;

  proofRejectionReason = '';
  proofError = '';
  proofSuccess = '';
  isProcessingProof = false;
  showRejectProofForm = false;

  showVoidConfirm = false;
  isVoiding = false;
  voidError = '';

  tabs: { key: TabKey; label: string; count?: number }[] = [
    { key: 'all',                 label: 'Alle' },
    { key: 'unpaid',              label: 'Ubetalt' },
    { key: 'pendingVerification', label: 'Bevis-gennemgang' },
    { key: 'paid',                label: 'Betalt' },
    { key: 'rejected',            label: 'Afvist' },
    { key: 'voided',              label: 'Annulleret' },
  ];

  readonly FineStatus = FineStatus;
  readonly FineType = FineType;

  constructor(
    private authService: AuthService,
    private fineService: FineService,
    public router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  get pageSize(): number {
    const available = window.innerHeight - 64 - 200 - 48 - 52 - 56 - 80;
    return Math.max(5, Math.floor(available / 88));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }

    this.loadFines();
    this.loadTabCounts();

    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadFines();
      });

    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFines(): void {
    this.isLoading = true;
    this.listError = null;

    const statusMap: Partial<Record<TabKey, FineStatus>> = {
      unpaid:              FineStatus.Unpaid,
      pendingVerification: FineStatus.PendingVerification,
      paid:                FineStatus.Paid,
      rejected:            FineStatus.Rejected,
      voided:              FineStatus.Voided,
    };

    const filter: FineFilter = {
      status: this.activeTab !== 'all' ? statusMap[this.activeTab] ?? null : null,
      search: this.searchQuery.trim() || null,
    };

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortDescending: true,
    };

    this.fineService
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
          this.fines = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          const tab = this.tabs.find((t) => t.key === this.activeTab);
          if (tab) tab.count = this.totalCount;
        },
        error: () => {
          this.listError = 'Kunne ikke hente bøder. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.fineService
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

    const statusTabs: { key: TabKey; status: FineStatus }[] = [
      { key: 'unpaid',              status: FineStatus.Unpaid },
      { key: 'pendingVerification', status: FineStatus.PendingVerification },
      { key: 'paid',                status: FineStatus.Paid },
      { key: 'rejected',            status: FineStatus.Rejected },
      { key: 'voided',              status: FineStatus.Voided },
    ];

    for (const { key, status } of statusTabs) {
      this.fineService
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
    this.loadFines();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadFines();
  }

  openModal(item: FineListDto): void {
    this.selectedItem = item;
    this.detail = null;
    this.showModal = true;
    this.isLoadingDetail = true;
    this.proofRejectionReason = '';
    this.proofError = '';
    this.proofSuccess = '';
    this.showRejectProofForm = false;
    this.showVoidConfirm = false;
    this.voidError = '';
    this.selectedPhoto = null;

    this.fineService
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

  get hasPendingProof(): boolean {
    return (
      this.detail?.status === FineStatus.PendingVerification &&
      !!this.detail?.paymentProofImageUrl
    );
  }

  approveProof(): void {
    if (!this.detail) return;
    this.isProcessingProof = true;
    this.proofError = '';

    this.fineService
      .adminVerifyPayment(this.detail.id, { isApproved: true })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isProcessingProof = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.proofSuccess = 'Betaling godkendt — bøden er markeret som betalt.';
          this.loadFines();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.proofSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.proofError = err.error?.message ?? 'Kunne ikke godkende betaling.';
        },
      });
  }

  rejectProof(): void {
    if (!this.detail || !this.proofRejectionReason.trim()) return;
    this.isProcessingProof = true;
    this.proofError = '';

    this.fineService
      .adminVerifyPayment(this.detail.id, {
        isApproved: false,
        rejectionReason: this.proofRejectionReason.trim(),
      })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isProcessingProof = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (res) => {
          this.detail = res.data!;
          this.proofSuccess = 'Betalingsbevis afvist.';
          this.showRejectProofForm = false;
          this.proofRejectionReason = '';
          this.loadFines();
          this.loadTabCounts();
          setTimeout(() => {
            this.showModal = false;
            this.proofSuccess = '';
            this.cdr.detectChanges();
          }, 1500);
        },
        error: (err) => {
          this.proofError = err.error?.message ?? 'Kunne ikke afvise betaling.';
        },
      });
  }

  get canVoid(): boolean {
    const s = this.detail?.status;
    return (
      s === FineStatus.Unpaid ||
      s === FineStatus.Rejected ||
      s === FineStatus.PendingVerification
    );
  }

  voidFine(): void {
    if (!this.detail) return;
    this.isVoiding = true;
    this.voidError = '';

    this.fineService
      .adminVoidFine(this.detail.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isVoiding = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.showVoidConfirm = false;
          this.showModal = false;
          this.loadFines();
          this.loadTabCounts();
        },
        error: (err) => {
          this.voidError = err.error?.message ?? 'Kunne ikke annullere bøde.';
        },
      });
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'badge badge-danger';
      case FineStatus.PendingVerification: return 'badge badge-warning';
      case FineStatus.Paid:                return 'badge badge-success';
      case FineStatus.Rejected:            return 'badge badge-danger';
      case FineStatus.Voided:              return 'badge';
      default:                             return 'badge';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'Ubetalt';
      case FineStatus.PendingVerification: return 'Afventer';
      case FineStatus.Paid:                return 'Betalt';
      case FineStatus.Rejected:            return 'Afvist';
      case FineStatus.Voided:              return 'Annulleret';
      default:                             return status;
    }
  }

  getTypeBadge(type: string): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'badge badge-warning';
      case FineType.Custom:            return 'badge badge-info';
      default:                         return 'badge';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'Tvistresultat';
      case FineType.Custom:            return 'Manuel';
      default:                         return type;
    }
  }

  getPaymentMethodLabel(method: string): string {
    switch (method) {
      case PaymentMethod.MobilePay:    return 'MobilePay';
      case PaymentMethod.Card:         return 'Kort';
      case PaymentMethod.BankTransfer: return 'Bankoverførsel';
      case PaymentMethod.Cash:         return 'Kontant';
      default:                         return method;
    }
  }

  getInitials(name: string): string {
    return name?.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}
