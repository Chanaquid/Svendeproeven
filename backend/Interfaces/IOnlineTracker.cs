namespace backend.Interfaces
{
    public interface IOnlineTracker
    {
        // Loan chat
        void AddToLoan(string connectionId, string userId, int loanId);
        bool IsUserInLoanGroup(string userId, int loanId);

        // Direct chat
        void AddToDirectChat(string connectionId, string userId, int conversationId);
        bool IsUserInDirectChat(string userId, int conversationId);

        // Support chat
        void AddToSupportChat(string connectionId, string userId, int ticketId);
        bool IsUserInSupportChat(string userId, int ticketId);

        // Called on any disconnect — cleans up all tracking for that connection
        void Remove(string connectionId);
    }
}