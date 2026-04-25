import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';

import { Navbar } from '../navbar/navbar';
import { CreateSupportThreadDto, SupportThreadDto, SupportThreadListDto } from '../../dtos/supportThreadDto';
import { SupportMessageDto } from '../../dtos/supportMessageDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { SupportThreadFilter } from '../../dtos/filterDto';
import { SupportThreadStatus } from '../../dtos/enums';
import { AuthService } from '../../services/authService';
import { SupportService } from '../../services/supportService';
import { SupportChatHubService } from '../../services/supportChatHubService';

@Component({
  selector: 'app-support-chat',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './support-chat.html',
  styleUrl: './support-chat.css',
})
export class SupportChat implements OnInit, OnDestroy {

  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;

  private destroy$ = new Subject<void>();
  private hubReconnectTimer: any = null;
  private hubHandlersRegistered = false;
  private joinedThreadId: number | null = null;

  threads: SupportThreadListDto[] = [];
  selectedThread: SupportThreadDto | null = null;
  messages: SupportMessageDto[] = [];

  isAdmin = false;
  currentUserId = '';

  isLoadingThreads = false;
  isLoadingThread = false;
  isSending = false;
  isCreatingThread = false;
  errorMessage = '';

  activeFilter: 'my' | 'unclaimed' | 'claimed' | 'closed' | 'all' = 'my';

  newMessage = '';
  showCreateForm = false;
  createSubject = '';
  createInitialMessage = '';

  // Thread pagination
  threadPage = 1;
  threadTotalCount = 0;

  // Message pagination
  messagePage = 1;
  messagePageSize = 30;
  messageTotalCount = 0;
  isLoadingMoreMessages = false;
  allMessagesLoaded = false;

  // Toast
  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  private threadRequest: PagedRequest = {
    page: 1,
    pageSize: 30,
    sortBy: 'CreatedAt',
    sortDescending: true,
  };

  constructor(
    private supportService: SupportService,
    private supportHubService: SupportChatHubService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    this.isAdmin = this.authService.isAdmin();
    this.currentUserId = this.authService.getCurrentUserId() ?? '';

    if (this.isAdmin) this.activeFilter = 'unclaimed';

    this.loadThreads();
    await this.initializeSignalR();
  }

  async ngOnDestroy(): Promise<void> {
    clearTimeout(this.hubReconnectTimer);
    clearTimeout(this.toastTimeout);
    if (this.joinedThreadId !== null) await this.safeLeaveHub(this.joinedThreadId);
    this.supportHubService.off();
    await this.supportHubService.stopConnection();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── SignalR ──────────────────────────────────────────────────────────────

  private async initializeSignalR(): Promise<void> {
    try {
      await this.supportHubService.startConnection();
      this.registerHubHandlers();
      this.supportHubService.onDisconnected(() => this.scheduleReconnect());
    } catch (error) {
      console.error('SupportHub connection failed:', error);
      this.scheduleReconnect();
    }
  }

  private registerHubHandlers(): void {
    if (this.hubHandlersRegistered) return;
    this.hubHandlersRegistered = true;

    this.supportHubService.onReceiveMessage((message: SupportMessageDto) => {
      message.isMine = message.senderId === this.currentUserId;

      if (this.messages.some(m => m.id === message.id)) return;

      if (message.supportThreadId === this.selectedThread?.id) {
        this.messages.push(message);
        this.markMessagesAsRead(message.supportThreadId);
        this.cdr.detectChanges();
        this.scrollToBottom();
      }

      this.updateThreadPreview(message);
      this.cdr.detectChanges();
    });

    this.supportHubService.onThreadUpdated((update: any) => {
      const thread = this.threads.find(t => t.id === update.threadId);
      if (!thread) {
        this.loadThreads();
        return;
      }

      thread.lastMessagePreview = update.lastMessagePreview;
      thread.lastMessageAt = update.lastMessageAt;

      const isCurrentThread = thread.id === this.selectedThread?.id;
      if (!isCurrentThread) thread.unreadCount = (thread.unreadCount ?? 0) + 1;

      this.threads = [thread, ...this.threads.filter(t => t.id !== thread.id)];
      this.cdr.detectChanges();
    });

    this.supportHubService.onError((error: string) => {
      this.showToast(error);
    });
  }

  private scheduleReconnect(): void {
    clearTimeout(this.hubReconnectTimer);
    this.hubReconnectTimer = setTimeout(async () => {
      try {
        if (this.supportHubService.state === signalR.HubConnectionState.Disconnected) {
          await this.supportHubService.startConnection();
          this.hubHandlersRegistered = false;
          this.registerHubHandlers();
        }
        if (this.joinedThreadId !== null) {
          await this.supportHubService.joinThread(this.joinedThreadId);
        }
      } catch {
        this.scheduleReconnect();
      }
    }, 3000);
  }

  private async safeJoinHub(threadId: number): Promise<void> {
    if (this.supportHubService.state !== signalR.HubConnectionState.Connected) return;
    if (this.joinedThreadId === threadId) return;
    if (this.joinedThreadId !== null) await this.safeLeaveHub(this.joinedThreadId);
    try {
      await this.supportHubService.joinThread(threadId);
      this.joinedThreadId = threadId;
    } catch (e) {
      console.error('Failed to join support hub:', e);
    }
  }

  private async safeLeaveHub(threadId: number): Promise<void> {
    if (this.supportHubService.state !== signalR.HubConnectionState.Connected) return;
    try { await this.supportHubService.leaveThread(threadId); } catch {}
    if (this.joinedThreadId === threadId) this.joinedThreadId = null;
  }

  // ─── Threads ──────────────────────────────────────────────────────────────

  get threadPageSize(): number {
    const available = window.innerHeight - 200;
    return Math.max(5, Math.floor(available / 72));
  }

  get totalThreadPages(): number {
    return Math.ceil(this.threadTotalCount / this.threadPageSize);
  }

  get threadPageNumbers(): number[] {
    const total = this.totalThreadPages;
    const current = this.threadPage;
    const pages: number[] = [];
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1);
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) pages.push(i);
      if (current < total - 2) pages.push(-1);
      pages.push(total);
    }
    return pages;
  }

  goToThreadPage(page: number): void {
    if (page < 1 || page > this.totalThreadPages) return;
    this.threadPage = page;
    this.loadThreads();
  }

  loadThreads(): void {
    this.isLoadingThreads = true;
    this.errorMessage = '';

    const filter = this.buildFilter();
    const request: PagedRequest = {
      ...this.threadRequest,
      page: this.threadPage,
      pageSize: this.threadPageSize,
    };

    const request$ = this.isAdmin && this.activeFilter !== 'my'
      ? this.supportService.adminGetAll(filter, request)
      : this.supportService.getMyThreads(filter, request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        this.threads = response.data?.items ?? [];
        this.threadTotalCount = response.data?.totalCount ?? 0;
        this.isLoadingThreads = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.errorMessage = error.error?.message ?? 'Failed to load support threads.';
        this.isLoadingThreads = false;
        this.cdr.detectChanges();
      },
    });
  }

  private buildFilter(): SupportThreadFilter {
    if (!this.isAdmin || this.activeFilter === 'my') return {};
    if (this.activeFilter === 'unclaimed') return { isUnclaimed: true, status: SupportThreadStatus.Open };
    if (this.activeFilter === 'claimed')   return { isClaimed: true, status: SupportThreadStatus.Claimed };
    if (this.activeFilter === 'closed')    return { status: SupportThreadStatus.Closed };
    return {};
  }

  changeFilter(filter: 'my' | 'unclaimed' | 'claimed' | 'closed' | 'all'): void {
    this.activeFilter = filter;
    this.threadPage = 1;
    this.loadThreads();
  }

  selectThread(thread: SupportThreadListDto): void {
    this.isLoadingThread = true;
    this.errorMessage = '';

    this.supportService.getById(thread.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        this.selectedThread = response.data ?? null;
        this.messages = [];
        this.isLoadingThread = false;
        this.safeJoinHub(thread.id);
        this.loadMessages(thread.id);
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.errorMessage = error.error?.message ?? 'Failed to load thread.';
        this.isLoadingThread = false;
        this.cdr.detectChanges();
      },
    });
  }

  // ─── Messages ─────────────────────────────────────────────────────────────

  loadMessages(threadId: number, prepend = false): void {
    if (prepend) {
      this.isLoadingMoreMessages = true;
    } else {
      this.isLoadingThread = true;
      this.messagePage = 1;
      this.allMessagesLoaded = false;
    }

    const request: PagedRequest = {
      page: this.messagePage,
      pageSize: this.messagePageSize,
      sortBy: 'sentAt',
      sortDescending: true,
    };

    this.supportService.getMessages(threadId, request).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const items = (res.data?.items ?? []).reverse();
        this.messageTotalCount = res.data?.totalCount ?? 0;

        if (prepend) {
          const container = this.messagesContainer?.nativeElement;
          const prevScrollHeight = container?.scrollHeight ?? 0;
          this.messages = [...items, ...this.messages];
          this.isLoadingMoreMessages = false;
          setTimeout(() => {
            if (container) container.scrollTop = container.scrollHeight - prevScrollHeight;
          }, 0);
        } else {
          this.messages = items;
          this.isLoadingThread = false;
          this.markMessagesAsRead(threadId);
          this.scrollToBottom();
        }

        if (this.messages.length >= this.messageTotalCount) this.allMessagesLoaded = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Failed to load messages.';
        this.isLoadingThread = false;
        this.isLoadingMoreMessages = false;
        this.cdr.detectChanges();
      },
    });
  }

  onMessagesScroll(event: Event): void {
    const el = event.target as HTMLDivElement;
    if (el.scrollTop < 50 && !this.isLoadingMoreMessages && !this.allMessagesLoaded) {
      if (!this.selectedThread) return;
      this.messagePage++;
      this.loadMessages(this.selectedThread.id, true);
    }
  }

  // ─── Actions ──────────────────────────────────────────────────────────────

  createThread(): void {
    const dto: CreateSupportThreadDto = {
      subject: this.createSubject.trim(),
      initialMessage: this.createInitialMessage.trim(),
    };
    if (!dto.subject || !dto.initialMessage || this.isCreatingThread) return;

    this.isCreatingThread = true;
    this.errorMessage = '';

    this.supportService.createThread(dto).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        const thread = response.data;
        if (!thread) {
          this.errorMessage = 'Thread response was empty.';
          this.isCreatingThread = false;
          this.cdr.detectChanges();
          return;
        }
        this.selectedThread = thread;
        this.messages = thread.messages ?? [];
        this.createSubject = '';
        this.createInitialMessage = '';
        this.showCreateForm = false;
        this.isCreatingThread = false;
        this.safeJoinHub(thread.id);
        this.loadThreads();
        this.cdr.detectChanges();
        this.scrollToBottom();
      },
      error: (error) => {
        this.errorMessage = error.error?.message ?? 'Failed to create thread.';
        this.isCreatingThread = false;
        this.cdr.detectChanges();
      },
    });
  }

  claimThread(): void {
    if (!this.selectedThread || !this.isAdmin) return;
    this.errorMessage = '';

    this.supportService.adminClaimThread(this.selectedThread.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.loadThreads(); this.refreshSelectedThread(); },
      error: (error) => { this.errorMessage = error.error?.message ?? 'Failed to claim thread.'; this.cdr.detectChanges(); },
    });
  }

  closeThread(): void {
    if (!this.selectedThread) return;
    this.errorMessage = '';

    this.supportService.closeThread(this.selectedThread.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.loadThreads(); this.refreshSelectedThread(); },
      error: (error) => { this.errorMessage = error.error?.message ?? 'Failed to close thread.'; this.cdr.detectChanges(); },
    });
  }

  sendMessage(): void {
    if (!this.selectedThread) return;
    const content = this.newMessage.trim();
    if (!content || this.isSending || this.selectedThread.status === SupportThreadStatus.Closed) return;

    this.isSending = true;
    this.errorMessage = '';

    this.supportService.sendMessage(this.selectedThread.id, { content }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        const message = response.data;
        if (!message) { this.isSending = false; this.cdr.detectChanges(); return; }

        this.newMessage = '';
        this.isSending = false;

        if (!this.messages.some(m => m.id === message.id)) {
          this.messages.push(message);
          this.scrollToBottom();
        }

        this.updateThreadPreview(message);
        this.loadThreads();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isSending = false;
        this.showToast(error.error?.message ?? 'Failed to send message.');
        this.cdr.detectChanges();
      },
    });
  }

  markMessagesAsRead(threadId: number): void {
    const lastMessageId = this.messages.length ? this.messages[this.messages.length - 1].id : null;
    if (!lastMessageId) return;

    this.supportService.markAsRead(threadId, { upToMessageId: lastMessageId }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        const thread = this.threads.find(t => t.id === threadId);
        if (thread) thread.unreadCount = 0;
        this.cdr.detectChanges();
      },
      error: () => {},
    });
  }

  refreshSelectedThread(): void {
    if (!this.selectedThread) return;

    this.supportService.getById(this.selectedThread.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        const thread = response.data;
        if (!thread) { this.selectedThread = null; this.messages = []; this.cdr.detectChanges(); return; }
        this.selectedThread = thread;
        const threadInList = this.threads.find(t => t.id === thread.id);
        if (threadInList) threadInList.status = thread.status;
        this.cdr.detectChanges();
      },
      error: (error) => { this.errorMessage = error.error?.message ?? 'Failed to refresh thread.'; this.cdr.detectChanges(); },
    });
  }

  // ─── Guards ───────────────────────────────────────────────────────────────

  canClaimSelectedThread(): boolean {
    return !!this.isAdmin && !!this.selectedThread &&
      this.selectedThread.status !== SupportThreadStatus.Closed &&
      !this.selectedThread.claimedByAdminId;
  }

  canCloseSelectedThread(): boolean {
    return !!this.isAdmin && !!this.selectedThread &&
      this.selectedThread.status !== SupportThreadStatus.Closed;
  }

  canSendMessage(): boolean {
    if (!this.selectedThread) return false;
    if (this.selectedThread.status === SupportThreadStatus.Closed) return false;
    if (!this.isAdmin) return true;
    return this.selectedThread.claimedByAdminId !== null;
  }

  hasActiveThread(): boolean {
    return this.threads.some(t =>
      t.status === SupportThreadStatus.Open || t.status === SupportThreadStatus.Claimed
    );
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  private updateThreadPreview(message: SupportMessageDto): void {
    const thread = this.threads.find(t => t.id === message.supportThreadId);
    if (!thread) { this.loadThreads(); return; }

    thread.lastMessagePreview = message.isMine ? `You: ${message.content}` : message.content;
    thread.lastMessageAt = message.sentAt;
    if (!message.isMine) thread.unreadCount = (thread.unreadCount ?? 0) + 1;

    this.threads = [thread, ...this.threads.filter(t => t.id !== thread.id)];
  }

  getThreadDisplayTitle(thread: SupportThreadListDto): string {
    return this.isAdmin ? `${thread.subject} — ${thread.userName}` : thread.subject;
  }

  getSelectedThreadTitle(): string {
    if (!this.selectedThread) return 'Support thread';
    return this.isAdmin
      ? `${this.selectedThread.subject} — ${this.selectedThread.userName}`
      : this.selectedThread.subject;
  }

  getStatusLabel(status: SupportThreadStatus): string { return status; }

  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ').filter(Boolean).map(p => p[0]).join('').toUpperCase().slice(0, 2);
  }

  formatMessageTime(value: string): string {
    if (!value) return '';
    const date = new Date(value);
    const isToday = date.toDateString() === new Date().toDateString();
    const time = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    return isToday ? time : date.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ' · ' + time;
  }

  formatThreadTime(value: string): string {
    if (!value) return '';
    const date = new Date(value);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffMins < 1) return 'now';
    if (diffMins < 60) return `${diffMins}m`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}d`;
    return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();
    clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => { this.toastVisible = false; this.cdr.detectChanges(); }, 2500);
  }

  trackThread(_: number, t: SupportThreadListDto): number { return t.id; }
  trackMessage(_: number, m: SupportMessageDto): number { return m.id; }

  private scrollToBottom(): void {
    setTimeout(() => {
      const container = this.messagesContainer?.nativeElement;
      if (container) container.scrollTop = container.scrollHeight;
    }, 0);
  }
}