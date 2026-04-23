import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FineStatus, FineType, PaymentMethod } from '../../dtos/enums';
import { debounceTime, distinctUntilChanged, finalize, Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FineDto, FineListDto, SubmitPaymentProofDto } from '../../dtos/fineDto';
import { FineService } from '../../services/fineService';
import { UploadImageService } from '../../services/uploadImageService';
import { Router } from '@angular/router';
import { PagedRequest } from '../../dtos/paginationDto';
import { FineFilter } from '../../dtos/filterDto';

type TabId = 'all' | 'unpaid' | 'rejected' | 'pending' | 'paid' | 'voided';

interface Tab {
  id: TabId;
  label: string;
  icon: string;
  status?: FineStatus;
  count?: number;
}

@Component({
  selector: 'app-fine',
  imports: [CommonModule, FormsModule],
  templateUrl: './fine.html',
  styleUrl: './fine.css',
})
export class Fine implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',      label: 'All',            icon: '▤' },
    { id: 'unpaid',   label: 'Unpaid',         icon: '🔴', status: FineStatus.Unpaid },
    { id: 'rejected', label: 'Rejected',       icon: '✕',  status: FineStatus.Rejected },
    { id: 'pending',  label: 'Pending Review', icon: '⏳', status: FineStatus.PendingVerification },
    { id: 'paid',     label: 'Paid',           icon: '✓',  status: FineStatus.Paid },
    { id: 'voided',   label: 'Voided',         icon: '—',  status: FineStatus.Voided },
  ];
  activeTab: TabId = 'all';

  // List state
  fines: FineListDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  searchQuery = '';
  sortFilter = 'newest';

  sortOptions = [
    { value: 'newest',       label: 'Newest first' },
    { value: 'oldest',       label: 'Oldest first' },
    { value: 'amount_desc',  label: 'Amount: High to Low' },
    { value: 'amount_asc',   label: 'Amount: Low to High' },
  ];

  // Detail state
  selectedId: number | null = null;
  selectedFine: FineDto | null = null;
  detailLoading = false;
  detailError: string | null = null;
  isRefreshingDetail = false;

  // Photo lightbox
  selectedPhoto: string | null = null;

  // Submit payment proof
  showProofForm = false;
  proofPaymentMethod: PaymentMethod = PaymentMethod.MobilePay;
  proofDescription = '';
  proofImageFile: File | null = null;
  proofImagePreview: string | null = null;
  isSubmittingProof = false;
  proofError = '';
  proofSuccess = '';
  uploadingProofImage = false;

  paymentMethods = [
    { value: PaymentMethod.MobilePay,    label: 'MobilePay' },
    { value: PaymentMethod.Card,         label: 'Card' },
    { value: PaymentMethod.BankTransfer, label: 'Bank Transfer' },
    { value: PaymentMethod.Cash,         label: 'Cash' },
  ];

  constructor(
    private fineService: FineService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.loadFines();
    this.loadTabCounts();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadFines();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFines(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find(t => t.id === this.activeTab)?.status ?? null;

    const filter: FineFilter = {
      search: this.searchQuery?.trim() || null,
      status: status,
    };

    const sortMap: Record<string, { sortBy: string; sortDescending: boolean }> = {
      newest:      { sortBy: 'createdAt', sortDescending: true },
      oldest:      { sortBy: 'createdAt', sortDescending: false },
      amount_desc: { sortBy: 'amount',    sortDescending: true },
      amount_asc:  { sortBy: 'amount',    sortDescending: false },
    };

    const { sortBy, sortDescending } = sortMap[this.sortFilter] ?? sortMap['newest'];

    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy,
      sortDescending,
    };

    this.fineService.getMyFines(filter, request)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.listLoading = false;
        this.isLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.fines = res.data.items;
            this.totalPages = res.data.totalPages;
            const tab = this.tabs.find(t => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Failed to load fines.';
          }
        },
        error: () => { this.listError = 'An error occurred. Please try again.'; },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.fineService.getMyFines(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => {
        const tab = this.tabs.find(t => t.id === 'all');
        if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
      }});

    const statusTabs: { id: TabId; status: FineStatus }[] = [
      { id: 'unpaid',   status: FineStatus.Unpaid },
      { id: 'rejected', status: FineStatus.Rejected },
      { id: 'pending',  status: FineStatus.PendingVerification },
      { id: 'paid',     status: FineStatus.Paid },
      { id: 'voided',   status: FineStatus.Voided },
    ];

    for (const { id, status } of statusTabs) {
      this.fineService.getMyFines({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({ next: (res) => {
          const tab = this.tabs.find(t => t.id === id);
          if (tab && res.data) { tab.count = res.data.totalCount; this.cdr.markForCheck(); }
        }});
    }
  }

  openFine(id: number): void {
    if (this.selectedId === id) return;

    // Keep previous fine visible while new one loads — no nulling
    const isFirstOpen = this.selectedFine === null;
    this.selectedId = id;
    this.detailError = null;
    this.resetProofForm();

    if (isFirstOpen) {
      this.detailLoading = true;
    } else {
      this.isRefreshingDetail = true;
    }

    this.fineService.getMyFineById(id)
      .pipe(takeUntil(this.destroy$), finalize(() => {
        this.detailLoading = false;
        this.isRefreshingDetail = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedFine = res.data;
          }
        },
        error: (err) => {
          this.selectedId = null;
          this.detailError = err.error?.message ?? 'Failed to load fine.';
          this.cdr.markForCheck();
        },
      });
  }

  private reloadFine(id: number): void {
    this.fineService.getMyFineById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.selectedFine = res.data;
            this.cdr.markForCheck();
          }
        }
      });
  }

  private resetProofForm(): void {
    this.showProofForm = false;
    this.proofPaymentMethod = PaymentMethod.MobilePay;
    this.proofDescription = '';
    this.proofImageFile = null;
    this.proofImagePreview = null;
    this.proofError = '';
    this.proofSuccess = '';
  }

  onProofImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.proofImageFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.proofImagePreview = e.target!.result as string;
      this.cdr.markForCheck();
    };
    reader.readAsDataURL(file);
    (event.target as HTMLInputElement).value = '';
  }

  async submitPaymentProof(): Promise<void> {
    if (!this.selectedFine || !this.proofImageFile || !this.proofDescription.trim()) {
      this.proofError = 'Please fill in all required fields and upload proof.';
      return;
    }

    this.isSubmittingProof = true;
    this.proofError = '';
    this.uploadingProofImage = true;

    try {
      const imageUrl = await this.uploadService.uploadImage(this.proofImageFile);
      this.uploadingProofImage = false;

      const dto: SubmitPaymentProofDto = {
        fineId: this.selectedFine.id,
        paymentMethod: this.proofPaymentMethod,
        paymentDescription: this.proofDescription.trim(),
        paymentProofImageUrl: imageUrl,
      };

      this.fineService.submitPaymentProof(dto)
        .pipe(takeUntil(this.destroy$), finalize(() => {
          this.isSubmittingProof = false;
          this.cdr.markForCheck();
        }))
        .subscribe({
          next: (res) => {
            if (res.success && res.data) {
              this.selectedFine = res.data;
              this.showProofForm = false;
              this.resetProofForm();
              this.proofSuccess = 'Payment proof submitted! Awaiting admin review.';
              this.loadFines();
              setTimeout(() => { this.proofSuccess = ''; this.cdr.markForCheck(); }, 4000);
            }
          },
          error: (err) => {
            this.proofError = err.error?.message ?? 'Failed to submit payment proof.';
          },
        });
    } catch {
      this.uploadingProofImage = false;
      this.isSubmittingProof = false;
      this.proofError = 'Failed to upload image.';
      this.cdr.markForCheck();
    }
  }

  canSubmitProof(): boolean {
    return this.selectedFine?.status === FineStatus.Unpaid ||
           this.selectedFine?.status === FineStatus.Rejected;
  }

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadFines();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  onFilterChange(): void { this.currentPage = 1; this.loadFines(); }
  goToPage(p: number): void { this.currentPage = p; this.loadFines(); }
  trackById(_: number, f: FineListDto): number { return f.id; }
  openPhoto(url: string): void { this.selectedPhoto = url; }

  getDefaultAvatar(name: string): string {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=27272a&color=a1a1aa&size=80`;
  }

  getStatusClass(status: FineStatus | string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'bg-red-400/10 text-red-400 border-red-400/20';
      case FineStatus.Rejected:            return 'bg-rose-400/10 text-rose-400 border-rose-400/20';
      case FineStatus.PendingVerification: return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case FineStatus.Paid:                return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case FineStatus.Voided:              return 'bg-zinc-700 text-zinc-400 border-zinc-700';
      default:                             return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getTypeLabel(type: FineType): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'Dispute Result';
      case FineType.Custom:            return 'Custom';
      default:                         return type;
    }
  }

  getTypeClass(type: FineType): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'bg-purple-400/10 text-purple-400';
      case FineType.Custom:            return 'bg-blue-400/10 text-blue-400';
      default:                         return 'bg-zinc-800 text-zinc-400';
    }
  }
}