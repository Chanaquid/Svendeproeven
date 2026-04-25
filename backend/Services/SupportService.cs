using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;

namespace backend.Services
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRepository;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<SupportChatHub> _hubContext;

        private const int InactiveHours = 48;

        public SupportService(
            ISupportRepository supportRepository,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IHubContext<SupportChatHub> hubContext)
        {
            _supportRepository = supportRepository;
            _userRepository = userRepository;
            _userManager = userManager;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        // ─── User actions ─────────────────────────────────────────────────────────

        public async Task<SupportThreadDto> CreateThreadAsync(string userId, CreateSupportThreadDto dto)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (await _supportRepository.HasActiveThreadAsync(userId))
                throw new InvalidOperationException("You already have an open support thread. Please wait for it to be resolved before opening a new one.");

            var thread = new SupportThread
            {
                UserId = userId,
                Subject = dto.Subject.Trim(),
                Status = SupportThreadStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _supportRepository.AddThreadAsync(thread);
            await _supportRepository.SaveChangesAsync();

            var message = new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = userId,
                Content = dto.InitialMessage.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _supportRepository.AddMessageAsync(message);
            await _supportRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.SupportThreadCreated,
                $"Your support thread '{thread.Subject}' has been created.",
                thread.Id,
                NotificationReferenceType.SupportThread
            );

            var created = await _supportRepository.GetThreadByIdWithMessagesAsync(thread.Id);
            return MapToDto(created!, userId);
        }

        public async Task<SupportThreadDto> GetThreadByIdAsync(int id, string userId, bool isAdmin)
        {
            var thread = await _supportRepository.GetThreadByIdWithMessagesAsync(id)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (!isAdmin && thread.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            return MapToDto(thread, userId);
        }

        public async Task<PagedResult<SupportThreadListDto>> GetMyThreadsAsync(
            string userId,
            SupportThreadFilter? filter,
            PagedRequest request)
        {
            var paged = await _supportRepository.GetThreadsByUserIdAsync(userId, filter, request);
            return await MapPagedToListDto(paged, userId);
        }

        public async Task<SupportMessageDto> SendMessageAsync(
            int threadId,
            string senderId,
            SendSupportMessageDto dto,
            bool isAdmin)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (!isAdmin && thread.UserId != senderId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("This support thread is closed. No further messages can be sent.");

            var message = new SupportMessage
            {
                SupportThreadId = threadId,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _supportRepository.AddMessageAsync(message);
            await _supportRepository.SaveChangesAsync();

            var recipientId = isAdmin ? thread.UserId : thread.ClaimedByAdminId;
            if (!string.IsNullOrEmpty(recipientId))
            {
                await _notificationService.SendAsync(
                    recipientId,
                    NotificationType.SupportMessageReceived,
                    $"New message in support thread: {thread.Subject}",
                    threadId,
                    NotificationReferenceType.SupportThread
                );
            }

            var sender = await _userRepository.GetByIdAsync(senderId);

            var messageDto = new SupportMessageDto
            {
                Id = message.Id,
                SupportThreadId = threadId,
                SenderId = senderId,
                SenderName = sender?.UserName ?? string.Empty,
                SenderFullName = sender?.FullName ?? string.Empty,
                SenderAvatarUrl = sender?.AvatarUrl,
                IsAdminMessage = isAdmin,
                IsMine = true,
                Content = message.Content,
                IsRead = false,
                SentAt = message.SentAt
            };

            // Broadcast to everyone viewing this thread
            await _hubContext.Clients
                .Group($"support_thread_{threadId}")
                .SendAsync("ReceiveMessage", messageDto);

            // Build preview for sidebar update
            var preview = message.Content.Length > 60
                ? message.Content[..60] + "…"
                : message.Content;

            var threadUpdate = new
            {
                threadId = thread.Id,
                lastMessagePreview = preview,
                lastMessageAt = message.SentAt,
                senderId = senderId,
            };

            // Notify the thread owner's sidebar
            await _hubContext.Clients
                .Group($"support_user_{thread.UserId}")
                .SendAsync("ThreadUpdated", threadUpdate);

            // Notify the claimed admin's sidebar (if different from sender)
            if (!string.IsNullOrEmpty(thread.ClaimedByAdminId) && thread.ClaimedByAdminId != senderId)
            {
                await _hubContext.Clients
                    .Group($"support_user_{thread.ClaimedByAdminId}")
                    .SendAsync("ThreadUpdated", threadUpdate);
            }

            return messageDto;
        }

        public async Task<PagedResult<SupportMessageDto>> GetMessagesAsync(
            int threadId,
            string userId,
            bool isAdmin,
            PagedRequest request)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (!isAdmin && thread.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            var paged = await _supportRepository.GetMessagesAsync(threadId, request);

            var items = paged.Items.Select(m => new SupportMessageDto
            {
                Id = m.Id,
                SupportThreadId = m.SupportThreadId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserName ?? string.Empty,
                SenderFullName = m.Sender?.FullName ?? string.Empty,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                IsAdminMessage = m.SenderId == thread.ClaimedByAdminId,
                IsMine = m.SenderId == userId,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            }).ToList();

            return new PagedResult<SupportMessageDto>
            {
                Items = items,
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task CloseThreadAsync(int threadId, string userId, bool isAdmin)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (!isAdmin && thread.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("This thread is already closed.");

            thread.Status = SupportThreadStatus.Closed;
            thread.ClosedAt = DateTime.UtcNow;

            _supportRepository.UpdateThread(thread);
            await _supportRepository.SaveChangesAsync();

            var recipientId = isAdmin ? thread.UserId : thread.ClaimedByAdminId;
            if (!string.IsNullOrEmpty(recipientId))
            {
                await _notificationService.SendAsync(
                    recipientId,
                    NotificationType.SupportThreadClosed,
                    $"Support thread '{thread.Subject}' has been closed.",
                    threadId,
                    NotificationReferenceType.SupportThread
                );
            }

            // Notify both parties that the thread status changed
            var statusUpdate = new { threadId = thread.Id, status = SupportThreadStatus.Closed.ToString() };

            await _hubContext.Clients
                .Group($"support_user_{thread.UserId}")
                .SendAsync("ThreadStatusUpdated", statusUpdate);

            if (!string.IsNullOrEmpty(thread.ClaimedByAdminId))
            {
                await _hubContext.Clients
                    .Group($"support_user_{thread.ClaimedByAdminId}")
                    .SendAsync("ThreadStatusUpdated", statusUpdate);
            }
        }

        public async Task MarkMessagesAsReadAsync(int threadId, string userId, MarkSupportMessagesReadDto dto)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (thread.UserId != userId && thread.ClaimedByAdminId != userId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            await _supportRepository.MarkMessagesAsReadAsync(threadId, userId, dto.UpToMessageId);
        }

        public async Task<bool> CanUserAccessThreadAsync(int threadId, string userId)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId);
            if (thread == null) return false;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            return isAdmin || thread.UserId == userId;
        }

        //Admin actions

        public async Task<SupportThreadDto> AdminCreateThreadAsync(
            string adminId,
            string targetUserId,
            CreateSupportThreadDto dto)
        {
            _ = await _userRepository.GetByIdAsync(targetUserId)
                ?? throw new KeyNotFoundException("Target user not found.");

            var thread = new SupportThread
            {
                UserId = targetUserId,
                Subject = dto.Subject.Trim(),
                Status = SupportThreadStatus.Claimed,
                ClaimedByAdminId = adminId,
                ClaimedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _supportRepository.AddThreadAsync(thread);
            await _supportRepository.SaveChangesAsync();

            var message = new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = adminId,
                Content = dto.InitialMessage.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _supportRepository.AddMessageAsync(message);
            await _supportRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                targetUserId,
                NotificationType.SupportThreadCreated,
                $"An admin has opened a support thread with you: '{thread.Subject}'",
                thread.Id,
                NotificationReferenceType.SupportThread
            );

            // Notify the user's sidebar of the new thread
            await _hubContext.Clients
                .Group($"support_user_{targetUserId}")
                .SendAsync("ThreadUpdated", new
                {
                    threadId = thread.Id,
                    lastMessagePreview = dto.InitialMessage.Length > 60
                        ? dto.InitialMessage[..60] + "…"
                        : dto.InitialMessage,
                    lastMessageAt = message.SentAt,
                    senderId = adminId,
                });

            var created = await _supportRepository.GetThreadByIdWithMessagesAsync(thread.Id);
            return MapToDto(created!, adminId);
        }

        public async Task<SupportThreadDto> ClaimThreadAsync(int threadId, string adminId)
        {
            var thread = await _supportRepository.GetThreadByIdWithMessagesAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("Cannot claim a closed thread.");

            if (thread.ClaimedByAdminId != null && thread.ClaimedByAdminId != adminId)
                throw new InvalidOperationException("This thread is already claimed by another admin.");

            if (thread.ClaimedByAdminId == adminId)
                throw new InvalidOperationException("You have already claimed this thread.");

            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new KeyNotFoundException("Admin not found.");

            thread.ClaimedByAdminId = adminId;
            thread.ClaimedAt = DateTime.UtcNow;
            thread.Status = SupportThreadStatus.Claimed;
            _supportRepository.UpdateThread(thread);

            var greeting = new SupportMessage
            {
                SupportThreadId = threadId,
                SenderId = adminId,
                Content = $"Hi, I'm {admin.FullName} from the RentIt support team. " +
                          $"I've picked up your thread and will be looking into this shortly. " +
                          $"Please feel free to provide any additional details that might help.",
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _supportRepository.AddMessageAsync(greeting);
            await _supportRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                thread.UserId,
                NotificationType.SupportThreadClaimed,
                $"{admin.FullName} from the support team has picked up your thread: '{thread.Subject}'",
                threadId,
                NotificationReferenceType.SupportThread
            );

            var greetingDto = new SupportMessageDto
            {
                Id = greeting.Id,
                SupportThreadId = threadId,
                SenderId = adminId,
                SenderName = admin.UserName ?? string.Empty,
                SenderFullName = admin.FullName ?? string.Empty,
                SenderAvatarUrl = admin.AvatarUrl,
                IsAdminMessage = true,
                IsMine = false,
                Content = greeting.Content,
                IsRead = false,
                SentAt = greeting.SentAt
            };

            //Push greeting message to the thread in real-time
            await _hubContext.Clients
                .Group($"support_thread_{threadId}")
                .SendAsync("ReceiveMessage", greetingDto);

            var preview = greeting.Content.Length > 60
                ? greeting.Content[..60] + "…"
                : greeting.Content;

            var threadUpdate = new
            {
                threadId = thread.Id,
                lastMessagePreview = preview,
                lastMessageAt = greeting.SentAt,
                senderId = adminId,
            };

            // Notify user's sidebar
            await _hubContext.Clients
                .Group($"support_user_{thread.UserId}")
                .SendAsync("ThreadUpdated", threadUpdate);

            // Notify status change
            var statusUpdate = new { threadId = thread.Id, status = SupportThreadStatus.Claimed.ToString() };

            await _hubContext.Clients
                .Group($"support_user_{thread.UserId}")
                .SendAsync("ThreadStatusUpdated", statusUpdate);

            var updated = await _supportRepository.GetThreadByIdWithMessagesAsync(threadId);
            return MapToDto(updated!, adminId);
        }

        public async Task<PagedResult<SupportThreadListDto>> GetAllThreadsAsync(
            SupportThreadFilter? filter,
            PagedRequest request)
        {
            var paged = await _supportRepository.GetAllThreadsAsync(filter, request);
            return await MapPagedToListDto(paged, currentUserId: null);
        }

        //Background job

        public async Task AutoCloseInactiveThreadsAsync()
        {
            var cutoff = DateTime.UtcNow.AddHours(-InactiveHours);
            var inactiveThreads = await _supportRepository.GetInactiveOpenThreadsAsync(cutoff);

            foreach (var thread in inactiveThreads)
            {
                thread.Status = SupportThreadStatus.Closed;
                thread.ClosedAt = DateTime.UtcNow;
                _supportRepository.UpdateThread(thread);

                await _notificationService.SendAsync(
                    thread.UserId,
                    NotificationType.SupportThreadClosed,
                    $"Your support thread '{thread.Subject}' was automatically closed due to inactivity.",
                    thread.Id,
                    NotificationReferenceType.SupportThread
                );

                var statusUpdate = new { threadId = thread.Id, status = SupportThreadStatus.Closed.ToString() };

                await _hubContext.Clients
                    .Group($"support_user_{thread.UserId}")
                    .SendAsync("ThreadStatusUpdated", statusUpdate);

                if (!string.IsNullOrEmpty(thread.ClaimedByAdminId))
                {
                    await _hubContext.Clients
                        .Group($"support_user_{thread.ClaimedByAdminId}")
                        .SendAsync("ThreadStatusUpdated", statusUpdate);
                }
            }

            await _supportRepository.SaveChangesAsync();
        }

        //Helpers

        private static SupportThreadDto MapToDto(SupportThread t, string? currentUserId)
        {
            return new SupportThreadDto
            {
                Id = t.Id,
                Subject = t.Subject,
                UserId = t.UserId,
                FullName = t.User?.FullName ?? string.Empty,
                UserName = t.User?.UserName ?? string.Empty,
                UserAvatarUrl = t.User?.AvatarUrl,
                ClaimedByAdminId = t.ClaimedByAdminId,
                ClaimedByAdminName = t.ClaimedByAdmin?.FullName,
                ClaimedByAdminUserName = t.ClaimedByAdmin?.UserName,
                ClaimedByAdminAvatarUrl = t.ClaimedByAdmin?.AvatarUrl,
                Status = t.Status,
                ClaimedAt = t.ClaimedAt,
                ClosedAt = t.ClosedAt,
                CreatedAt = t.CreatedAt,
                Messages = t.Messages.Select(m => new SupportMessageDto
                {
                    Id = m.Id,
                    SupportThreadId = m.SupportThreadId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.UserName ?? string.Empty,
                    SenderFullName = m.Sender?.FullName ?? string.Empty,
                    SenderAvatarUrl = m.Sender?.AvatarUrl,
                    IsAdminMessage = m.SenderId == t.ClaimedByAdminId,
                    IsMine = currentUserId != null && m.SenderId == currentUserId,
                    Content = m.Content,
                    IsRead = m.IsRead,
                    SentAt = m.SentAt
                }).ToList()
            };
        }

        private async Task<PagedResult<SupportThreadListDto>> MapPagedToListDto(
            PagedResult<SupportThread> source,
            string? currentUserId)
        {
            var items = new List<SupportThreadListDto>();

            foreach (var t in source.Items)
            {
                var lastMessage = await _supportRepository.GetLastMessageAsync(t.Id);
                var unreadCount = currentUserId != null
                    ? await _supportRepository.GetUnreadCountAsync(t.Id, currentUserId)
                    : 0;

                items.Add(new SupportThreadListDto
                {
                    Id = t.Id,
                    Subject = t.Subject,
                    FullName = t.User?.FullName ?? string.Empty,
                    UserName = t.User?.UserName ?? string.Empty,
                    UserAvatarUrl = t.User?.AvatarUrl,
                    ClaimedByAdminName = t.ClaimedByAdmin?.FullName,
                    Status = t.Status,
                    LastMessagePreview = lastMessage != null
                        ? lastMessage.Content.Length > 100
                            ? lastMessage.Content[..100] + "..."
                            : lastMessage.Content
                        : null,
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = unreadCount,
                    CreatedAt = t.CreatedAt
                });
            }

            return new PagedResult<SupportThreadListDto>
            {
                Items = items,
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}