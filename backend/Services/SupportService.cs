using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        private const int InactiveHours = 48;

        public SupportService(
            ISupportRepository supportRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _supportRepository = supportRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        //User actions

        public async Task<SupportThreadDto> CreateThreadAsync(string userId, CreateSupportThreadDto dto)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            //Users can only have one active (non-closed) thread at a time
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

            //Add initial message
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

            //Users can only see their own threads; admins can see all
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

            //Only the thread owner or any admin can message
            if (!isAdmin && thread.UserId != senderId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            //Closed threads accept no messages from anyone
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

            //Notify the other party
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

            return new SupportMessageDto
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
        }


        //Admin or threadowner can close the thread
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

            //Notify the other party
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
        }

        public async Task MarkMessagesAsReadAsync(int threadId, string userId, MarkSupportMessagesReadDto dto)
        {
            var thread = await _supportRepository.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException("Support thread not found.");

            if (thread.UserId != userId && thread.ClaimedByAdminId != userId)
                throw new UnauthorizedAccessException("You do not have access to this thread.");

            await _supportRepository.MarkMessagesAsReadAsync(threadId, userId, dto.UpToMessageId);
        }

        //Admins

        public async Task<SupportThreadDto> AdminCreateThreadAsync(
            string adminId,
            string targetUserId,
            CreateSupportThreadDto dto)
        {
            _ = await _userRepository.GetByIdAsync(targetUserId)
                ?? throw new KeyNotFoundException("Target user not found.");

            //Admins bypass the one-active-thread restriction
            var thread = new SupportThread
            {
                UserId = targetUserId,
                Subject = dto.Subject.Trim(),
                Status = SupportThreadStatus.Claimed, //Admin-created threads are immediately claimed
                ClaimedByAdminId = adminId,
                ClaimedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _supportRepository.AddThreadAsync(thread);
            await _supportRepository.SaveChangesAsync();

            //Admin's initial message
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

            //Automatic greeting message from admin
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
                $"{admin.FullName} from the support team has picked up your thread: '{thread.Subject}'", threadId,
                NotificationReferenceType.SupportThread
            );

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