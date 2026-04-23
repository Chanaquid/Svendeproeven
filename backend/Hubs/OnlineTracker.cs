using backend.Interfaces;

namespace backend.Hubs
{
    public class OnlineTracker : IOnlineTracker
    {
        private readonly Dictionary<string, (string UserId, string GroupType, int GroupId)> _connections = new();
        private readonly object _lock = new();

        public void AddToLoan(string connectionId, string userId, int loanId)
        {
            lock (_lock) _connections[connectionId] = (userId, "loan", loanId);
        }

        public void AddToDirectChat(string connectionId, string userId, int conversationId)
        {
            lock (_lock) _connections[connectionId] = (userId, "direct", conversationId);
        }

        public void AddToSupportChat(string connectionId, string userId, int ticketId)
        {
            lock (_lock) _connections[connectionId] = (userId, "support", ticketId);
        }

        public void Remove(string connectionId)
        {
            lock (_lock) _connections.Remove(connectionId);
        }

        public bool IsUserInLoanGroup(string userId, int loanId)
        {
            lock (_lock) return _connections.Values.Any(v => v.UserId == userId && v.GroupType == "loan" && v.GroupId == loanId);
        }

        public bool IsUserInDirectChat(string userId, int conversationId)
        {
            lock (_lock) return _connections.Values.Any(v => v.UserId == userId && v.GroupType == "direct" && v.GroupId == conversationId);
        }

        public bool IsUserInSupportChat(string userId, int ticketId)
        {
            lock (_lock) return _connections.Values.Any(v => v.UserId == userId && v.GroupType == "support" && v.GroupId == ticketId);
        }
    }
}