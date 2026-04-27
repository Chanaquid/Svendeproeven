import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize, takeUntil } from 'rxjs/operators';

import { FineService } from '../../services/fineService';
import { UploadImageService } from '../../services/uploadImageService';
import { FineDto, FineListDto, SubmitPaymentProofDto } from '../../dtos/fineDto';
import { FineStatus, FineType, PaymentMethod } from '../../dtos/enums';
import { FineFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';

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
export class Fine implements OnInit, OnChanges, OnDestroy {
  @Input() openFineId: number | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();
  private resizeHandler = () => { this.currentPage = 1; this.loadFines(); };

  isLoading = true;

  tabs: Tab[] = [
    { id: 'all',      label: 'Alle',           icon: '▤' },
    { id: 'unpaid',   label: 'Ubetalt',        icon: '🔴', status: FineStatus.Unpaid },
    { id: 'rejected', label: 'Afvist',         icon: '✕',  status: FineStatus.Rejected },
    { id: 'pending',  label: 'Afventer',       icon: '⏳', status: FineStatus.PendingVerification },
    { id: 'paid',     label: 'Betalt',         icon: '✓',  status: FineStatus.Paid },
    { id: 'voided',   label: 'Annulleret',     icon: '—',  status: FineStatus.Voided },
  ];
  activeTab: TabId = 'all';

  fines: FineListDto[] = [];
  listLoading = false;
  listError: string | null = null;
  currentPage = 1;
  totalCount = 0;
  searchQuery = '';
  sortFilter = 'newest';

  sortOptions = [
    { value: 'newest',      label: 'Nyeste først' },
    { value: 'oldest',      label: 'Ældste først' },
    { value: 'amount_desc', label: 'Beløb: høj til lav' },
    { value: 'amount_asc',  label: 'Beløb: lav til høj' },
  ];

  selectedId: number | null = null;
  selectedFine: FineDto | null = null;
  detailLoading = false;
  detailError: string | null = null;
  isRefreshingDetail = false;

  selectedPhoto: string | null = null;

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
    { value: PaymentMethod.Card,         label: 'Kort' },
    { value: PaymentMethod.BankTransfer, label: 'Bankoverførsel' },
    { value: PaymentMethod.Cash,         label: 'Kontant' },
  ];

  readonly FineStatus = FineStatus;

  constructor(
    private fineService: FineService,
    private uploadService: UploadImageService,
    private cdr: ChangeDetectorRef,
    public router: Router,
  ) {}

  get pageSize(): number {
    const availableHeight = window.innerHeight - 64 - 52 - 48 - 80;
    return Math.max(5, Math.floor(availableHeight / 100));
  }

  get totalPages(): number { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  ngOnInit(): void {
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['openFineId'] && this.openFineId) {
      this.openFine(this.openFineId);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeHandler);
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFines(): void {
    if (this.isLoading) this.listLoading = true;
    this.listError = null;

    const status = this.tabs.find((t) => t.id === this.activeTab)?.status ?? null;

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

    this.fineService
      .getMyFines(filter, request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.listLoading = false;
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.fines = res.data.items;
            this.totalCount = res.data.totalCount;
            const tab = this.tabs.find((t) => t.id === this.activeTab);
            if (tab) tab.count = res.data.totalCount;
          } else {
            this.listError = res.message || 'Kunne ikke hente bøder.';
          }
        },
        error: () => {
          this.listError = 'Der opstod en fejl. Prøv igen.';
        },
      });
  }

  private loadTabCounts(): void {
    const request: PagedRequest = { page: 1, pageSize: 1, sortBy: 'createdAt', sortDescending: true };

    this.fineService
      .getMyFines(null, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const tab = this.tabs.find((t) => t.id === 'all');
          if (tab && res.data) {
            tab.count = res.data.totalCount;
            this.cdr.markForCheck();
          }
        },
      });

    const statusTabs: { id: TabId; status: FineStatus }[] = [
      { id: 'unpaid',   status: FineStatus.Unpaid },
      { id: 'rejected', status: FineStatus.Rejected },
      { id: 'pending',  status: FineStatus.PendingVerification },
      { id: 'paid',     status: FineStatus.Paid },
      { id: 'voided',   status: FineStatus.Voided },
    ];

    for (const { id, status } of statusTabs) {
      this.fineService
        .getMyFines({ status }, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            const tab = this.tabs.find((t) => t.id === id);
            if (tab && res.data) {
              tab.count = res.data.totalCount;
              this.cdr.markForCheck();
            }
          },
        });
    }
  }

  openFine(id: number): void {
    if (this.selectedId === id) return;

    const isFirstOpen = this.selectedFine === null;
    this.selectedId = id;
    this.detailError = null;
    this.resetProofForm();

    if (isFirstOpen) this.detailLoading = true;
    else this.isRefreshingDetail = true;

    this.fineService
      .getMyFineById(id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.detailLoading = false;
          this.isRefreshingDetail = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) this.selectedFine = res.data;
        },
        error: (err) => {
          this.selectedId = null;
          this.detailError = err.error?.message ?? 'Kunne ikke indlæse bøde.';
          this.cdr.markForCheck();
        },
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
      this.proofError = 'Udfyld alle felter og vedhæft et bevis.';
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

      this.fineService
        .submitPaymentProof(dto)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => {
            this.isSubmittingProof = false;
            this.cdr.markForCheck();
          }),
        )
        .subscribe({
          next: (res) => {
            if (res.success && res.data) {
              this.selectedFine = res.data;
              this.showProofForm = false;
              this.resetProofForm();
              this.proofSuccess = 'Betalingsbevis indsendt! Afventer admin-gennemgang.';
              this.loadFines();
              setTimeout(() => {
                this.proofSuccess = '';
                this.cdr.markForCheck();
              }, 4000);
            }
          },
          error: (err) => {
            this.proofError = err.error?.message ?? 'Kunne ikke indsende bevis.';
          },
        });
    } catch {
      this.uploadingProofImage = false;
      this.isSubmittingProof = false;
      this.proofError = 'Kunne ikke uploade billede.';
      this.cdr.markForCheck();
    }
  }

  canSubmitProof(): boolean {
    return (
      this.selectedFine?.status === FineStatus.Unpaid ||
      this.selectedFine?.status === FineStatus.Rejected
    );
  }

  switchTab(tab: TabId): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.loadFines();
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  onFilterChange(): void { this.currentPage = 1; this.loadFines(); }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.currentPage = p;
    this.loadFines();
  }

  trackById(_: number, f: FineListDto): number { return f.id; }
  openPhoto(url: string): void { this.selectedPhoto = url; }

  getStatusBadge(status: FineStatus | string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'badge badge-danger';
      case FineStatus.Rejected:            return 'badge badge-danger';
      case FineStatus.PendingVerification: return 'badge badge-warning';
      case FineStatus.Paid:                return 'badge badge-success';
      case FineStatus.Voided:              return 'badge';
      default:                             return 'badge';
    }
  }

  getStatusLabel(status: FineStatus | string): string {
    switch (status) {
      case FineStatus.Unpaid:              return 'Ubetalt';
      case FineStatus.Rejected:            return 'Afvist';
      case FineStatus.PendingVerification: return 'Afventer';
      case FineStatus.Paid:                return 'Betalt';
      case FineStatus.Voided:              return 'Annulleret';
      default:                             return status as string;
    }
  }

  getTypeLabel(type: FineType): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'Tvistresultat';
      case FineType.Custom:            return 'Manuel';
      default:                         return type;
    }
  }

  getTypeBadge(type: FineType): string {
    switch (type) {
      case FineType.ResultedByDispute: return 'badge badge-warning';
      case FineType.Custom:            return 'badge badge-info';
      default:                         return 'badge';
    }
  }

  getPaymentMethodLabel(method: PaymentMethod | string): string {
    switch (method) {
      case PaymentMethod.MobilePay:    return 'MobilePay';
      case PaymentMethod.Card:         return 'Kort';
      case PaymentMethod.BankTransfer: return 'Bankoverførsel';
      case PaymentMethod.Cash:         return 'Kontant';
      default:                         return method as string;
    }
  }
}
