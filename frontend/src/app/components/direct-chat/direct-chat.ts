import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

import { Navbar } from '../navbar/navbar';
import { DirectConversationDto, DirectConversationListDto } from '../../dtos/directConversationDto';
import { DirectMessageDto } from '../../dtos/directMessageDto';
import { UserFilter } from '../../dtos/filterDto';
import { PagedRequest } from '../../dtos/paginationDto';
import { UserProfileDto } from '../../dtos/userDto';
import { ConversationService } from '../../services/conversationService';
import { UserService } from '../../services/userService';
import { DirectChatHubService } from '../../services/directChatHubService';
import { AuthService } from '../../services/authService';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-direct-chat',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './direct-chat.html',
  styleUrl: './direct-chat.css',
})
export class DirectChat implements OnInit, OnDestroy {

  @ViewChild('messagesContainer') private messagesContainer?: ElementRef<HTMLDivElement>;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  currentUserId = '';
  currentUserAvatarUrl: string | null = null;
  currentUserFullName = '';
  isAdmin = false;

  conversations: DirectConversationListDto[] = [];
  selectedConversation: DirectConversationListDto | null = null;
  selectedConversationDetails: DirectConversationDto | null = null;
  messages: DirectMessageDto[] = [];

  newMessage = '';
  isLoadingConversations = false;
  isLoadingMessages = false;
  isSending = false;
  errorMessage = '';

  // Toast (#2)
  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  // Conversation pagination
  conversationPage = 1;
  conversationTotalCount = 0;
  isLoadingMoreConversations = false;

  // User search
  userSearch = '';
  foundUsers: UserProfileDto[] = [];
  isSearchingUsers = false;
  showUserResults = false;
  isStartingConversation = false;

  // Avatar/profile dialog
  showAvatarDialog: 'mine' | 'other' | null = null;
  lightboxUrl: string | null = null;


  showDeleteConfirmId: number | null = null;


  private joinedConversationId: number | null = null;
  private hubReconnectTimer: any = null;
  private hubHandlersRegistered = false;

  messagePage = 1;
  messagePageSize = 30;
  messageTotalCount = 0;
  isLoadingMoreMessages = false;
  allMessagesLoaded = false;

  //filter admins from user search, but admins can search all
  private readonly userSearchRequest: PagedRequest = {
    page: 1, pageSize: 5, sortBy: 'Username', sortDescending: false,
  };

  constructor(
    private conversationService: ConversationService,
    private userService: UserService,
    private chatHubService: DirectChatHubService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  // ─── Lifecycle ────────────────────────────────────────────────────────────────

  async ngOnInit(): Promise<void> {
    this.isAdmin = this.authService.isAdmin();

    this.userService.getMyProfile().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.currentUserId = res.data?.id ?? '';
        this.currentUserAvatarUrl = res.data?.avatarUrl ?? null;
        this.currentUserFullName = res.data?.fullName ?? '';
        this.cdr.detectChanges();
      }
    });

    this.loadConversations();
    await this.initializeSignalR();

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(search => this.executeUserSearch(search));

    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const conversationId = params['conversationId'];
        const otherUserId = params['otherUserId'];

        if (conversationId) {
          this.openConversationById(+conversationId);
        } else if (otherUserId) {
          this.openOrCreateConversation(otherUserId);
        }
    });

    
  }

  async ngOnDestroy(): Promise<void> {
    clearTimeout(this.hubReconnectTimer);
    clearTimeout(this.toastTimeout);
    if (this.joinedConversationId !== null) await this.safeLeaveHub(this.joinedConversationId);
    this.chatHubService.off();
    await this.chatHubService.stopConnection();
    this.destroy$.next();
    this.destroy$.complete();
  }

  openConversationById(conversationId: number): void {
    if (!conversationId) return;

    this.conversationService.getById(conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const conversation = res.data;
          if (!conversation) return;

          this.selectedConversationDetails = conversation;
          const existing = this.conversations.find(c => c.id === conversation.id);
          this.selectedConversation = existing ?? this.mapDetailsToList(conversation);
          this.messages = [];

          this.safeJoinHub(conversationId);
          this.loadMessages(conversationId);
          // Removed: this.loadConversations() — no need to reload the whole list
          this.cdr.detectChanges();
        },
        error: () => {
          this.showToast('Failed to open conversation.');
        }
      });
  }

  // ─── SignalR (#5 real-time fix) ───────────────────────────────────────────────

  private async initializeSignalR(): Promise<void> {
    try {
      await this.chatHubService.startConnection();
      this.registerHubHandlers();
      this.chatHubService.onDisconnected(() => this.scheduleReconnect());
    } catch (error) {
      console.error('SignalR connection failed:', error);
      this.scheduleReconnect();
    }
  }

  private registerHubHandlers(): void {
    // Prevent double-registration on reconnect
    if (this.hubHandlersRegistered) return;
    this.hubHandlersRegistered = true;

    this.chatHubService.onReceiveMessage((message: DirectMessageDto) => {

      message.isMine = message.senderId === this.currentUserId;

      const currentId = this.selectedConversation?.id ?? this.selectedConversationDetails?.id;

      if (this.messages.some(m => m.id === message.id)) return;

      if (message.conversationId === currentId) {
        this.messages.push(message);
        this.markConversationAsRead(message.conversationId);
        this.cdr.detectChanges();
        this.scrollToBottom();
      }

      this.updateConversationPreview(message);
      this.cdr.detectChanges();
    });

    this.chatHubService.onConversationUpdated((update: any) => {
        let conv = this.conversations.find(c => c.id === update.conversationId);
        if (!conv) {
          // Unknown conversation — reload to get it
          this.loadConversations();
          return;
        }

        const isCurrentConversation = conv.id === (this.selectedConversation?.id ?? this.selectedConversationDetails?.id);

           conv.lastMessageContent = update.lastMessageContent; 
          conv.lastMessageSentAt = update.lastMessageSentAt;
          conv.lastMessageSenderId = update.senderId;
          conv.lastMessageSenderName = update.senderFullName;
          conv.lastMessageAvatarUrl = update.senderAvatarUrl;

          if (!isCurrentConversation) {
            conv.unreadCount = (conv.unreadCount ?? 0) + 1;
          }

        // Bubble to top
          this.conversations = [conv, ...this.conversations.filter(c => c.id !== conv!.id)];
          this.cdr.detectChanges();
    });

    this.chatHubService.onError((error: string) => {
      this.showToast(error);
    });
  }

  private scheduleReconnect(): void {
    clearTimeout(this.hubReconnectTimer);
    this.hubReconnectTimer = setTimeout(async () => {
      try {
        if (this.chatHubService.state === signalR.HubConnectionState.Disconnected) {
          await this.chatHubService.startConnection();
          this.hubHandlersRegistered = false; // allow re-registration after fresh start
          this.registerHubHandlers();
        }
        if (this.joinedConversationId !== null) {
          await this.chatHubService.joinConversation(this.joinedConversationId);
        }
      } catch {
        this.scheduleReconnect();
      }
    }, 3000);
  }

  private async safeJoinHub(conversationId: number): Promise<void> {
    if (this.chatHubService.state !== signalR.HubConnectionState.Connected) return;
    if (this.joinedConversationId === conversationId) return;
    if (this.joinedConversationId !== null) await this.safeLeaveHub(this.joinedConversationId);
    try {
      await this.chatHubService.joinConversation(conversationId);
      this.joinedConversationId = conversationId;
    } catch (e) {
      console.error('Failed to join hub:', e);
    }
  }

  private async safeLeaveHub(conversationId: number): Promise<void> {
    if (this.chatHubService.state !== signalR.HubConnectionState.Connected) return;
    try { await this.chatHubService.leaveConversation(conversationId); } catch {}
    if (this.joinedConversationId === conversationId) this.joinedConversationId = null;
  }

  // ─── Conversations ────────────────────────────────────────────────────────────

  loadConversations(append = false): void {
    if (append) {
      this.isLoadingMoreConversations = true;
    } else {
      this.isLoadingConversations = true;
      this.conversationPage = 1;
    }

    const request: PagedRequest = {
      page: this.conversationPage,
      pageSize: this.conversationPageSize,
    };

    this.conversationService.getMyConversations({}, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const items = res.data?.items ?? [];
          this.conversationTotalCount = res.data?.totalCount ?? 0;
          console.log('Loaded conversations:', res);

          // Sort by lastMessageSentAt descending, nulls last
          const sorted = items.sort((a, b) => {
            if (!a.lastMessageSentAt) return 1;
            if (!b.lastMessageSentAt) return -1;
            return new Date(b.lastMessageSentAt).getTime() - new Date(a.lastMessageSentAt).getTime();
          });

          this.conversations = append ? [...this.conversations, ...sorted] : sorted;
          this.isLoadingConversations = false;
          this.isLoadingMoreConversations = false;

          if (this.selectedConversationDetails) {
            const match = this.conversations.find(c => c.id === this.selectedConversationDetails?.id);
            if (match) this.selectedConversation = match;
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.errorMessage = err.error?.message ?? 'Failed to load conversations.';
          this.isLoadingConversations = false;
          this.isLoadingMoreConversations = false;
          this.cdr.detectChanges();
        },
      });
  }

  selectConversation(conversation: DirectConversationListDto): void {
    this.selectedConversation = conversation;
    this.selectedConversationDetails = null;
    this.messages = [];
    this.loadConversationDetails(conversation.id);
    this.loadMessages(conversation.id);
    this.safeJoinHub(conversation.id);
  }

  private loadConversationDetails(conversationId: number): void {
    this.conversationService.getById(conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => { this.selectedConversationDetails = res.data ?? null; this.cdr.detectChanges(); },
        error: () => {},
      });
  }

  openOrCreateConversation(otherUserId: string): void {
    const userId = otherUserId.trim();
    if (!userId || this.isStartingConversation) return;

    this.isStartingConversation = true;
    this.errorMessage = '';

    this.conversationService.getOrCreate(userId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const conversation = res.data;
          if (!conversation) {
            this.errorMessage = 'Conversation data was empty.';
            this.isStartingConversation = false;
            this.cdr.detectChanges();
            return;
          }
          this.selectedConversationDetails = conversation;
          const existing = this.conversations.find(c => c.id === conversation.id);
          this.selectedConversation = existing ?? this.mapDetailsToList(conversation);
          this.messages = conversation.messages ?? [];
          this.userSearch = '';
          this.foundUsers = [];
          this.showUserResults = false;
          this.isStartingConversation = false;
          this.safeJoinHub(conversation.id);
          this.loadMessages(conversation.id);

          if (!this.conversations.find(c => c.id === conversation.id)) {
            const listItem = this.mapDetailsToList(conversation);
            this.conversations = [listItem, ...this.conversations];
            this.conversationTotalCount++;
          }

          
          this.cdr.detectChanges();
          this.scrollToBottom();
        },
        error: (err) => {
          this.errorMessage = err.error?.message ?? 'Failed to open conversation.';
          this.isStartingConversation = false;
          this.cdr.detectChanges();
        },
      });
  }

  confirmDeleteConversation(conversationId: number): void {
    this.showDeleteConfirmId = conversationId;
  }

  cancelDelete(): void {
    this.showDeleteConfirmId = null;
  }


  //admin can delete conversations from user's DM list via same endpoint
  deleteConversation(conversationId: number): void {
    this.showDeleteConfirmId = null;
    this.conversationService.delete(conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.conversations = this.conversations.filter(c => c.id !== conversationId);
          this.conversationTotalCount = Math.max(0, this.conversationTotalCount - 1);
          const selectedId = this.selectedConversation?.id ?? this.selectedConversationDetails?.id;
          if (selectedId === conversationId) {
            this.selectedConversation = null;
            this.selectedConversationDetails = null;
            this.messages = [];
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.showToast(err.error?.message ?? 'Failed to delete conversation.');
        },
      });
  }

  // ─── Messages ─────────────────────────────────────────────────────────────────

  get conversationPageSize(): number {
    const availableHeight = window.innerHeight - 200; // navbar + header + new chat section
    const itemHeight = 72; // approx height of each conversation item
    return Math.max(5, Math.floor(availableHeight / itemHeight));
  }

  get totalConversationPages(): number {
    return Math.ceil(this.conversationTotalCount / this.conversationPageSize);
  }

  get conversationPageNumbers(): number[] {
    const total = this.totalConversationPages;
    const current = this.conversationPage;
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

  goToConversationPage(page: number): void {
    if (page < 1 || page > this.totalConversationPages) return;
    this.conversationPage = page;
    this.loadConversations();
  }


  loadMessages(conversationId: number, prepend = false): void {
    if (prepend) {
      this.isLoadingMoreMessages = true;
    } else {
      this.isLoadingMessages = true;
      this.messagePage = 1;
      this.allMessagesLoaded = false;
    }

    const request: PagedRequest = {
      page: this.messagePage,
      pageSize: this.messagePageSize,
      sortBy: 'sentAt',
      sortDescending: true, // newest first from API, we'll reverse
    };

    this.conversationService.getMessages(conversationId, {}, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const items = (res.data?.items ?? []).reverse(); // reverse so oldest is first
          this.messageTotalCount = res.data?.totalCount ?? 0;

          if (prepend) {
            const container = this.messagesContainer?.nativeElement;
            const prevScrollHeight = container?.scrollHeight ?? 0;
            this.messages = [...items, ...this.messages];
            this.isLoadingMoreMessages = false;
            // Restore scroll position after prepend
            setTimeout(() => {
              if (container) container.scrollTop = container.scrollHeight - prevScrollHeight;
            }, 0);
          } else {
            this.messages = items;
            this.isLoadingMessages = false;
            this.markConversationAsRead(conversationId);
            this.scrollToBottom();
          }

          if (this.messages.length >= this.messageTotalCount) {
            this.allMessagesLoaded = true;
          }

          this.cdr.detectChanges();
        },
        error: (err) => {
          this.errorMessage = err.error?.message ?? 'Failed to load messages.';
          this.isLoadingMessages = false;
          this.isLoadingMoreMessages = false;
          this.cdr.detectChanges();
        },
      });
  }

  onMessagesScroll(event: Event): void {
    const el = event.target as HTMLDivElement;
    if (el.scrollTop < 50 && !this.isLoadingMoreMessages && !this.allMessagesLoaded) {
      const convId = this.selectedConversation?.id ?? this.selectedConversationDetails?.id;
      if (!convId) return;
      this.messagePage++;
      this.loadMessages(convId, true);
    }
  }


  sendMessage(): void {
    const conversationId = this.selectedConversation?.id ?? this.selectedConversationDetails?.id;
    if (!conversationId) return;
    const content = this.newMessage.trim();
    if (!content || this.isSending) return;

    // #2 — for blocked check on frontend, just show toast without revealing reason
    if (this.selectedConversationDetails?.isBlocked) {
      this.showToast('Unable to send message.');
      return;
    }

    this.isSending = true;

    this.conversationService.sendMessage(conversationId, { content })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const sent = res.data;
          if (!sent) { this.isSending = false; this.cdr.detectChanges(); return; }

          this.newMessage = '';
          this.isSending = false;

          // Optimistically add to messages
          if (!this.messages.some(m => m.id === sent.id)) {
            this.messages.push(sent);
            this.scrollToBottom();
          }

          const conv = this.conversations.find(c => c.id === conversationId);
          if (conv) {
            conv.lastMessageContent = `You: ${sent.content}`;
            conv.lastMessageSentAt = sent.sentAt;
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isSending = false;
          // #2 — generic toast, never reveal block reason to blocked person
          this.showToast('Unable to send message.');
          this.cdr.detectChanges();
        },
      });
  }

  markConversationAsRead(conversationId: number): void {
    this.conversationService.markAsRead(conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const conv = this.conversations.find(c => c.id === conversationId);
          if (conv) conv.unreadCount = 0;
          if (this.selectedConversationDetails?.id === conversationId) {
            this.selectedConversationDetails.unreadCount = 0;
          }
          this.cdr.detectChanges();
        },
        error: () => {},
      });
  }

  // ─── User search (#3 filter admins for non-admin users) ───────────────────────

  onUserSearchChange(): void {
    const search = this.userSearch.trim();
    if (!search) {
      this.foundUsers = [];
      this.showUserResults = false;
      this.isSearchingUsers = false;
      this.cdr.detectChanges();
      return;
    }
    this.isSearchingUsers = true;
    this.showUserResults = true;
    this.cdr.detectChanges();
    this.searchSubject.next(search);
  }

  private executeUserSearch(search: string): void {
    // #3 — non-admins should only see non-admin users
    const filter: UserFilter = {
      search,
      ...(!this.isAdmin && { excludeAdmins: true }),
    };

    this.userService.searchUsers(filter, this.userSearchRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          let users = res.data?.items ?? [];
          this.foundUsers = users;
          this.isSearchingUsers = false;
          this.showUserResults = true;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.foundUsers = [];
          this.isSearchingUsers = false;
          this.showToast(err.error?.message ?? 'Failed to search users.');
          this.cdr.detectChanges();
        },
      });
  }

  selectFoundUser(user: UserProfileDto): void {
    this.userSearch = user.username;
    this.foundUsers = [];
    this.showUserResults = false;
    this.openOrCreateConversation(user.id);
  }

  clearUserResults(): void {
    setTimeout(() => { this.showUserResults = false; this.cdr.detectChanges(); }, 150);
  }

  // ─── Avatar / profile dialog ──────────────────────────────────────────────────

  openAvatarDialog(who: 'mine' | 'other'): void { this.showAvatarDialog = who; }
  closeAvatarDialog(): void { this.showAvatarDialog = null; }

  viewAvatarFullscreen(): void {
    const url = this.showAvatarDialog === 'mine'
      ? this.currentUserAvatarUrl
      : this.getCurrentConversationAvatar();
    if (url) this.lightboxUrl = url;
    this.showAvatarDialog = null;
  }

  visitUserPage(): void {
    const userId = this.showAvatarDialog === 'mine'
      ? this.currentUserId
      : (this.selectedConversation?.otherUserId ?? this.selectedConversationDetails?.otherUserId);
    this.showAvatarDialog = null;
    if (userId) this.router.navigate(['/users', userId]);
  }

  closeLightbox(): void { this.lightboxUrl = null; }

  // ─── UI helpers ───────────────────────────────────────────────────────────────

  getCurrentConversationName(): string {
    return this.selectedConversation?.otherUserName
      || this.selectedConversationDetails?.otherUserName
      || 'Conversation';
  }

  getCurrentConversationAvatar(): string | null {
    return this.selectedConversation?.otherUserAvatarUrl
      || this.selectedConversationDetails?.otherUserAvatarUrl
      || null;
  }

  getConversationPreview(conversation: DirectConversationListDto): string {
    if (conversation.isBlocked) return '[Blocked User]';
    return conversation.lastMessageContent || 'No messages yet';
  }

  isOwnMessage(message: DirectMessageDto): boolean {
    return message.senderId === this.currentUserId;
  }

  getMessageAvatar(message: DirectMessageDto): string | null {
    if (this.isOwnMessage(message)) return this.currentUserAvatarUrl;
    return message.senderAvatarUrl
      || this.selectedConversation?.otherUserAvatarUrl
      || this.selectedConversationDetails?.otherUserAvatarUrl
      || null;
  }

  getMessageSenderName(message: DirectMessageDto): string {
    return this.isOwnMessage(message)
      ? 'You'
      : (message.senderFullName || this.getCurrentConversationName());
  }

  getMessageInitials(message: DirectMessageDto): string {
    if (this.isOwnMessage(message)) return this.getInitials(this.currentUserFullName || 'You');
    return this.getInitials(message.senderFullName || this.getCurrentConversationName());
  }

  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ').filter(Boolean).map(p => p[0]).join('').toUpperCase().slice(0, 2);
  }

  formatMessageTime(value: string): string {
    if (!value) return '';
    const date = new Date(value);
    const today = new Date();
    const isToday = date.toDateString() === today.toDateString();
    
    const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    if (isToday) return timeStr;
    
    // If not today, show short date + time
    return date.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ' · ' + timeStr;
  }

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();
    clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 2000);
  }

  trackConversation(_: number, c: DirectConversationListDto): number { return c.id; }
  trackMessage(_: number, m: DirectMessageDto): number { return m.id; }
  trackUser(_: number, u: UserProfileDto): string { return u.id; }

  private scrollToBottom(): void {
    setTimeout(() => {
      const container = this.messagesContainer?.nativeElement;
      if (container) container.scrollTop = container.scrollHeight;
    }, 0);
  }

  private updateConversationPreview(message: DirectMessageDto): void {
    let conv = this.conversations.find(c => c.id === message.conversationId);
    if (!conv) { this.loadConversations(); return; }

    conv.lastMessageContent = this.isOwnMessage(message) ? `You: ${message.content}` : message.content;
    conv.lastMessageSentAt = message.sentAt;
    conv.lastMessageSenderId = message.senderId;
    conv.lastMessageSenderName = message.senderFullName;
    conv.lastMessageAvatarUrl = message.senderAvatarUrl;
    if (!this.isOwnMessage(message)) conv.unreadCount += 1;

    this.conversations = [conv, ...this.conversations.filter(c => c.id !== conv!.id)];
  }

  private mapDetailsToList(conversation: DirectConversationDto): DirectConversationListDto {
    const lastMsg = conversation.messages?.length
      ? conversation.messages[conversation.messages.length - 1] : null;
    return {
      id: conversation.id,
      otherUserId: conversation.otherUserId,
      otherUserFullName: conversation.otherUserFullName,
      otherUserName: conversation.otherUserName,
      otherUserAvatarUrl: conversation.otherUserAvatarUrl,
      lastMessageContent: lastMsg?.content ?? null,
      lastMessageSentAt: conversation.lastMessageAt,
      lastMessageSenderId: lastMsg?.senderId ?? null,
      lastMessageSenderName: lastMsg?.senderFullName ?? null,
      lastMessageAvatarUrl: lastMsg?.senderAvatarUrl ?? null,
      isBlocked: conversation.isBlocked,
      unreadCount: conversation.unreadCount,
      createdAt: conversation.createdAt,
      isInitiatedByMe: conversation.initiatedById !== conversation.otherUserId,
    };
  }

  formatConversationTime(value: string): string {
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
}