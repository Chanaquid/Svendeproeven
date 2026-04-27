using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class DirectMessageRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly DirectMessageRepository _repo;

        public DirectMessageRepositoryTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new TestApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repo = new DirectMessageRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
        }

        //Helpers

        private ApplicationUser CreateUser(string id)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = id,
                Email = $"{id}@test.com",
                FullName = $"User {id}"
            };
        }

        private async Task<DirectConversation> CreateConversationAsync(string initiatorId, string otherId)
        {
            _context.Users.AddRange(CreateUser(initiatorId), CreateUser(otherId));
            await _context.SaveChangesAsync();

            var conversation = new DirectConversation
            {
                InitiatedById = initiatorId,
                OtherUserId = otherId,
                HiddenForInitiator = false,
                HiddenForOther = false
            };

            _context.DirectConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        //GetConversationMessagesAsync

        [Fact]
        public async Task GetConversationMessagesAsync_ReturnsMessages_WhenUserIsParticipant()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv.Id, SenderId = "u1", Content = "hello", SentAt = DateTime.UtcNow },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", Content = "hi", SentAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _repo.GetConversationMessagesAsync(
                conv.Id, "u1", null,
                new PagedRequest { Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetConversationMessagesAsync_ReturnsEmpty_WhenConversationHidden()
        {
            _context.Users.AddRange(CreateUser("u1"), CreateUser("u2"));
            await _context.SaveChangesAsync();

            _context.DirectConversations.Add(new DirectConversation
            {
                InitiatedById = "u1",
                OtherUserId = "u2",
                HiddenForInitiator = true
            });
            await _context.SaveChangesAsync();

            var conv = await _context.DirectConversations.FirstAsync();

            var result = await _repo.GetConversationMessagesAsync(
                conv.Id, "u1", null,
                new PagedRequest { Page = 1, PageSize = 10 });

            Assert.Equal(0, result.TotalCount);
        }

        //GetUnreadCountForConversationAsync

        [Fact]
        public async Task GetUnreadCountForConversationAsync_ReturnsCorrectCount()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u1", IsRead = false }
            );
            await _context.SaveChangesAsync();

            var count = await _repo.GetUnreadCountForConversationAsync(conv.Id, "u1");

            Assert.Equal(2, count);
        }

        //GetTotalUnreadCountForUserAsync

        [Fact]
        public async Task GetTotalUnreadCountForUserAsync_ReturnsOnlyIncomingUnread()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u1", IsRead = false }
            );
            await _context.SaveChangesAsync();

            var result = await _repo.GetTotalUnreadCountForUserAsync("u1");

            Assert.Equal(2, result);
        }

        //GetLastMessageAsync

        [Fact]
        public async Task GetLastMessageAsync_ReturnsMostRecentMessage()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv.Id, SenderId = "u1", SentAt = DateTime.UtcNow.AddMinutes(-10) },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", SentAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var messages = await _context.DirectMessages.OrderBy(m => m.SentAt).ToListAsync();

            var result = await _repo.GetLastMessageAsync(conv.Id);

            Assert.Equal(messages[1].Id, result!.Id);
        }

        //GetMessageCountAsync

        [Fact]
        public async Task GetMessageCountAsync_ReturnsCorrectCount()
        {
            var conv1 = await CreateConversationAsync("u1", "u2");

            _context.Users.AddRange(CreateUser("u3"), CreateUser("u4"));
            await _context.SaveChangesAsync();
            _context.DirectConversations.Add(new DirectConversation { InitiatedById = "u3", OtherUserId = "u4" });
            await _context.SaveChangesAsync();
            var conv2 = await _context.DirectConversations.OrderBy(c => c.Id).LastAsync();

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv1.Id, SenderId = "u1" },
                new DirectMessage { ConversationId = conv1.Id, SenderId = "u2" },
                new DirectMessage { ConversationId = conv2.Id, SenderId = "u3" }
            );
            await _context.SaveChangesAsync();

            var count = await _repo.GetMessageCountAsync(conv1.Id);

            Assert.Equal(2, count);
        }

        //MarkMessagesAsReadAsync

        [Fact]
        public async Task MarkMessagesAsReadAsync_UpdatesMessages()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            _context.DirectMessages.AddRange(
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false },
                new DirectMessage { ConversationId = conv.Id, SenderId = "u2", IsRead = false }
            );
            await _context.SaveChangesAsync();

            await _repo.MarkMessagesAsReadAsync(conv.Id, "u1");

            _context.ChangeTracker.Clear();

            var messages = await _context.DirectMessages.ToListAsync();

            Assert.All(messages, m => Assert.True(m.IsRead));
        }

        //AddAsync

        [Fact]
        public async Task AddAsync_AddsMessageToDbSet()
        {
            var conv = await CreateConversationAsync("u1", "u2");

            await _repo.AddAsync(new DirectMessage
            {
                ConversationId = conv.Id,
                SenderId = "u1",
                Content = "test"
            });
            await _context.SaveChangesAsync();

            Assert.Equal(1, await _context.DirectMessages.CountAsync());
        }
    }
}