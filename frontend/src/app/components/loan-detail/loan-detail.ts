import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Navbar } from '../navbar/navbar';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LoanDto } from '../../dtos/loanDto';
import { LoanMessageDto } from '../../dtos/loanMessageDto';
import { AuthService } from '../../services/authService';
import { LoanService } from '../../services/loanService';
import { LoanMessageService } from '../../services/loanMessageService';
import { UserService } from '../../services/userService';
import { ItemReviewService } from '../../services/itemReviewService';
import { ItemService } from '../../services/itemService';
import { DisputeService } from '../../services/disputeService';
import { UserReviewService } from '../../services/userReviewService';
import { LoanChatHubService } from '../../services/loanChatHubService';


@Component({
  selector: 'app-loan-detail',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './loan-detail.html',
  styleUrl: './loan-detail.css',
})
export class LoanDetail implements OnInit, OnDestroy {
  @ViewChild('messageContainer') messageContainer!: ElementRef;

  loan: LoanDto | null = null;
  messages: LoanMessageDto[] = [];
  currentUserId = '';
  isLoading = true;
  isSending = false;
  newMessage = '';
  selectedPhoto: string | null = null;
  private loanId = 0;

  isAdmin = false;

  // QR code
  showQrModal = false;
  qrCode = '';
  isLoadingQr = false;

  // Decision
  decisionNote = '';
  decisionError = '';
  isDeciding = false;

  // Reviews
  showReviewItem = false;
  showReviewUser = false;
  itemReviewRating = 0;
  itemReviewComment = '';
  userReviewRating = 0;
  userReviewComment = '';
  isSubmittingItemReview = false;
  isSubmittingUserReview = false;
  itemReviewError = '';
  itemReviewSuccess = '';
  userReviewError = '';
  userReviewSuccess = '';
  hasReviewedItem = false;
  hasReviewedUser = false;

  // Loan activation / completion
  isActivatingLoan = false;
  isCompletingLoan = false;
  activateError = '';
  completeError = '';
  qrCodeInput = '';

  // Loan cancel
  isCancellingLoan = false;
  cancelLoanError = '';
  showCancelConfirm = false;

  // Dispute
  showDisputeModal = false;
  disputeForm = { description: '', photoUrl: '', photoCaption: '' };
  isFilingDispute = false;
  disputeError = '';

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private authService: AuthService,
    private loanService: LoanService,
    private loanMessageService: LoanMessageService,
    private userService: UserService,
    private itemReviewService: ItemReviewService,
    private userReviewService: UserReviewService,
    private itemService: ItemService,
    private disputeService: DisputeService,
    private loanChatHubService: LoanChatHubService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.loanId = Number(this.route.snapshot.paramMap.get('id'));

    this.userService.getMyProfile().subscribe({
      next: (res) => {
        this.currentUserId = res.data?.id ?? '';
        this.loadLoan(this.loanId);
      },
      error: () => {
        this.loadLoan(this.loanId);
      },
    });
  }

  ngOnDestroy(): void {
    this.loanChatHubService.leaveLoan(this.loanId);
    this.loanChatHubService.off();
    this.loanChatHubService.stop();
  }

  private startSignalR(loanId: number): void {
    this.loanChatHubService.onReceiveMessage((msg: LoanMessageDto) => {
      const exists = this.messages.some((m) => m.id === msg.id);
      if (!exists) {
        this.messages.push(msg);
        this.cdr.detectChanges();
        this.triggerScroll();
        if (msg.senderId !== this.currentUserId) {
          this.loanMessageService
            .markAsRead(loanId, { upToMessageId: msg.id })
            .subscribe();
        }
      }
      this.cdr.detectChanges();
    });

    this.loanChatHubService.onMessageRead((messageId: number) => {
      const msg = this.messages.find((m) => m.id === messageId);
      if (msg && msg.senderId === this.currentUserId) {
        msg.isRead = true;
        this.cdr.detectChanges();
      }
    });

    this.loanChatHubService
      .start()
      .then(() =>
        this.loanChatHubService
          .joinLoan(loanId)
          .catch((err) => console.warn('Could not join loan group:', err)),
      )
      .catch((err) => console.error('SignalR connection failed:', err));
  }

  private loadLoan(id: number): void {
    this.loanService.getById(id).subscribe({
      next: (res) => {
        this.loan = res.data;
        this.isLoading = false;
        this.cdr.detectChanges();
        this.loadMessages(id);
        this.startSignalR(id);
        const status = this.loan?.status;
        if (status === 'Completed' || status === 'Late') {
          this.checkExistingReviews();
        }
      },
      error: () => {
        this.loan = null;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private loadMessages(loanId: number): void {
    this.loanMessageService
      .getMessages(loanId, { page: 1, pageSize: 100 })
      .subscribe({
        next: (res) => {
          this.messages = res.data?.items ?? [];
          this.cdr.detectChanges();
          this.triggerScroll();
        },
        error: () => {},
      });
  }

  get isChatLocked(): boolean {
    if (!this.loan) return false;
    const terminalStatuses: string[] = ['Completed', 'Cancelled', 'Rejected'];
    if (!terminalStatuses.includes(this.loan.status)) return false;
    const lockedAt = this.loan.actualReturnDate ?? this.loan.createdAt;
    return new Date(lockedAt) < new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
  }

  private checkExistingReviews(): void {
    if (!this.loan) return;

    this.itemReviewService
      .getByItem(this.loan.itemId, {}, { page: 1, pageSize: 100 })
      .subscribe({
        next: (res) => {
          this.hasReviewedItem =
            res.data?.items.some(
              (r) => r.reviewerId === this.currentUserId,
            ) ?? false;
          this.cdr.detectChanges();
        },
        error: () => {},
      });

    const otherPartyId = this.isOwner
      ? this.loan.borrowerId
      : this.loan.lenderId;

    this.userReviewService
      .getReviewsForUser(otherPartyId, {}, { page: 1, pageSize: 100 })
      .subscribe({
        next: (res) => {
          this.hasReviewedUser =
            res.data?.items.some(
              (r) => r.reviewerId === this.currentUserId,
            ) ?? false;
          this.cdr.detectChanges();
        },
        error: () => {},
      });
  }

  private triggerScroll(): void {
    setTimeout(() => {
      this.scrollToBottom();
      this.cdr.detectChanges();
    }, 50);
  }

  private scrollToBottom(): void {
    try {
      const el = this.messageContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    } catch {}
  }

  trackMessage(_index: number, msg: LoanMessageDto): number {
    return msg.id;
  }

  openQrModal(): void {
    this.showQrModal = true;
    if (this.qrCode) return;
    this.isLoadingQr = true;

    this.itemService.getQrCode(this.loan!.itemId).subscribe({
      next: (res) => {
        this.qrCode = res.data?.qrCode ?? '';
        this.isLoadingQr = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingQr = false;
        this.cdr.detectChanges();
      },
    });
  }

  decide(isApproved: boolean): void {
    if (!this.loan) return;
    this.isDeciding = true;
    this.decisionError = '';

    this.loanService
      .decide(this.loan.id, {
        loanId: this.loan.id,
        isApproved,
        decisionNote: this.decisionNote,
      })
      .subscribe({
        next: (res) => {
          this.loan = res.data;
          this.isDeciding = false;
          this.decisionNote = '';
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.decisionError = err.error?.message ?? 'Failed to process decision.';
          this.isDeciding = false;
          this.cdr.detectChanges();
        },
      });
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.loan || this.isSending) return;
    this.isSending = true;
    const content = this.newMessage.trim();
    this.newMessage = '';

    this.loanMessageService
      .sendMessage(this.loan.id, { content })
      .subscribe({
        next: (res) => {
          // Optimistically push the sent message immediately
          if (res?.data) {
            const exists = this.messages.some((m) => m.id === res.data?.id);
            if (!exists) {
              this.messages.push(res.data);
              this.cdr.detectChanges();
              this.triggerScroll();
            }
          }
          this.isSending = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.newMessage = content;
          this.isSending = false;
          this.cdr.detectChanges();
        },
      });
  }

  activateLoan(): void {
    if (!this.qrCodeInput.trim()) return;
    this.isActivatingLoan = true;
    this.activateError = '';

    this.loanService
      .confirmPickup({ qrCode: this.qrCodeInput.trim().toUpperCase() })
      .subscribe({
        next: () => {
          this.isActivatingLoan = false;
          this.qrCodeInput = '';
          this.loadLoan(this.loan!.id);
        },
        error: (err) => {
          this.activateError = err.error?.message ?? 'Invalid QR code.';
          this.isActivatingLoan = false;
          this.cdr.detectChanges();
        },
      });
  }

  cancelLoan(): void {
    if (!this.loan) return;
    this.isCancellingLoan = true;
    this.cancelLoanError = '';

    this.loanService
      .cancel(this.loan.id, { loanId: this.loan.id, reason: '' })
      .subscribe({
        next: () => {
          this.isCancellingLoan = false;
          this.showCancelConfirm = false;
          this.loadLoan(this.loan!.id);
        },
        error: (err) => {
          this.cancelLoanError = err.error?.message ?? 'Failed to cancel loan.';
          this.isCancellingLoan = false;
          this.cdr.detectChanges();
        },
      });
  }

  completeLoan(): void {
    if (!this.qrCodeInput.trim()) return;
    this.isCompletingLoan = true;
    this.completeError = '';

    this.loanService
      .confirmReturn({ qrCode: this.qrCodeInput.trim().toUpperCase() })
      .subscribe({
        next: () => {
          this.isCompletingLoan = false;
          this.qrCodeInput = '';
          this.loadLoan(this.loan!.id);
        },
        error: (err) => {
          this.completeError = err.error?.message ?? 'Invalid QR code.';
          this.isCompletingLoan = false;
          this.cdr.detectChanges();
        },
      });
  }

  fileDispute(): void {
    if (!this.loan || !this.disputeForm.description.trim()) {
      this.disputeError = 'Please describe the issue.';
      return;
    }
    if (this.disputeForm.description.trim().length < 20) {
      this.disputeError = 'Description must be at least 20 characters.';
      return;
    }
    this.isFilingDispute = true;
    this.disputeError = '';

    const filedAs = this.effectiveRole === 'Owner' ? 'AsOwner' : 'AsBorrower';

    this.disputeService
      .createDispute({
        loanId: this.loan.id,
        filedAs: filedAs as any,
        description: this.disputeForm.description.trim(),
      })
      .subscribe({
        next: (res) => {
          const created = res.data!;
          if (this.disputeForm.photoUrl.trim()) {
            this.disputeService
              .addFiledByPhoto(created.id, {
                photoUrl: this.disputeForm.photoUrl.trim(),
                caption: this.disputeForm.photoCaption.trim() || undefined,
              })
              .subscribe({
                next: () => this.onDisputeSuccess(),
                error: () => this.onDisputeSuccess(),
              });
          } else {
            this.onDisputeSuccess();
          }
        },
        error: (err) => {
          this.disputeError = err.error?.message ?? 'Failed to file dispute.';
          this.isFilingDispute = false;
          this.cdr.detectChanges();
        },
      });
  }

  private onDisputeSuccess(): void {
    this.isFilingDispute = false;
    this.showDisputeModal = false;
    this.disputeForm = { description: '', photoUrl: '', photoCaption: '' };
    this.cdr.detectChanges();
    this.loadLoan(this.loan!.id);
  }

  setItemRating(r: number): void {
    this.itemReviewRating = r;
  }

  setUserRating(r: number): void {
    this.userReviewRating = r;
  }

  submitItemReview(): void {
    if (!this.loan || this.itemReviewRating === 0) {
      this.itemReviewError = 'Please select a rating.';
      return;
    }
    this.isSubmittingItemReview = true;
    this.itemReviewError = '';

    this.itemReviewService
      .create(this.loan.itemId, {
        itemId: this.loan.itemId,
        loanId: this.loan.id,
        rating: this.itemReviewRating,
        comment: this.itemReviewComment.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.hasReviewedItem = true;
          this.isSubmittingItemReview = false;
          this.itemReviewSuccess = '✓ Item review submitted!';
          this.showReviewItem = false;
          this.itemReviewRating = 0;
          this.itemReviewComment = '';

          setTimeout(() => {
            this.itemReviewSuccess = '';
            this.cdr.detectChanges();
          }, 2000);

          this.cdr.detectChanges();
        },
        error: (err) => {
          this.itemReviewError = err.error?.message ?? 'Failed to submit review.';
          this.isSubmittingItemReview = false;
          this.cdr.detectChanges();
        },
      });
  }

  submitUserReview(): void {
    if (!this.loan || this.userReviewRating === 0) {
      this.userReviewError = 'Please select a rating.';
      return;
    }
    this.isSubmittingUserReview = true;
    this.userReviewError = '';

    const reviewedUserId = this.isOwner
      ? this.loan.borrowerId
      : this.loan.lenderId;

    this.userReviewService
      .createReview({
        loanId: this.loan.id,
        rating: this.userReviewRating,
        comment: this.userReviewComment.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.hasReviewedUser = true;
          this.isSubmittingUserReview = false;
          this.userReviewSuccess = '✓ User review submitted!';
          this.showReviewUser = false;
          this.userReviewRating = 0;
          this.userReviewComment = '';

          setTimeout(() => {
          this.userReviewSuccess = '';
          this.cdr.detectChanges();
        }, 2000);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.userReviewError = err.error?.message ?? 'Failed to submit review.';
          this.isSubmittingUserReview = false;
          this.cdr.detectChanges();
        },
      });
  }

  get isOwner(): boolean {
    return this.loan?.lenderId === this.currentUserId;
  }

  get otherPartyName(): string {
    if (!this.loan) return '';
    return this.isOwner ? this.loan.borrowerName : this.loan.lenderName;
  }

  get otherPartyUserName(): string {
    if (!this.loan) return '';
    return this.isOwner ? this.loan.borrowerUserName : this.loan.lenderUserName;
  }

  get otherPartyAvatarUrl(): string | null {
    if (!this.loan) return null;
    return this.isOwner ? this.loan.borrowerAvatarUrl : this.loan.lenderAvatarUrl;
  }

  get otherPartyId(): string {
    if (!this.loan) return '';
    return this.isOwner ? this.loan.borrowerId : this.loan.lenderId;
  }

  get effectiveRole(): 'Admin' | 'Owner' | 'Borrower' {
    if (this.isOwner) return 'Owner';
    if (this.loan?.borrowerId === this.currentUserId) return 'Borrower';
    if (this.isAdmin) return 'Admin';
    return 'Borrower';
  }

  goToItem(): void {
    if (this.loan) this.router.navigate(['/items', this.loan.itemSlug]);
  }

  openPhoto(url: string): void {
    this.selectedPhoto = url;
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

  getLoanStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'bg-emerald-400/10 text-emerald-400 border-emerald-400/20';
      case 'approved':
        return 'bg-blue-400/10 text-blue-400 border-blue-400/20';
      case 'completed':
        return 'bg-cyan-300/10 text-cyan-300 border-cyan-300/20';
      case 'late':
      case 'overdue':
        return 'bg-red-400/10 text-red-400 border-red-400/20';
      case 'pending':
        return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'adminpending':
        return 'bg-amber-400/10 text-amber-400 border-amber-400/20';
      case 'cancelled':
      case 'rejected':
        return 'bg-rose-400/10 text-rose-400 border-rose-400/20';
      default:
        return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  getConditionClass(condition: string): string {
    switch (condition?.toLowerCase()) {
      case 'excellent':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'good':
        return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'fair':
        return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'poor':
        return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      default:
        return 'bg-zinc-800 text-zinc-400 border-zinc-700';
    }
  }

  get isDisputeLocked(): boolean {
    if (!this.loan) return false;
    if (this.loan.status !== 'Completed') return false;
    const completedAt = this.loan.actualReturnDate ?? this.loan.updatedAt;
    if (!completedAt) return false;
    return new Date(completedAt) < new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
  }
}