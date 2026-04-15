using backend.Interfaces;
using backend.Models;

namespace backend.BackgroundServices
{
    public class AutoExpirePendingLoansService : BackgroundService
    {
        private readonly ILogger<AutoExpirePendingLoansService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        //Pending loans expire after 48hrs with no action
        private const int PendingExpiryHours = 48;

        public AutoExpirePendingLoansService(
            ILogger<AutoExpirePendingLoansService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoExpirePendingLoansService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoExpirePendingLoansService");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var loanRepo = scope.ServiceProvider.GetRequiredService<ILoanRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var cutoff = DateTime.UtcNow.AddHours(-PendingExpiryHours);
            var expiredLoans = await loanRepo.GetExpiredPendingLoansAsync(cutoff);

            foreach (var loan in expiredLoans)
            {
                loan.Status = LoanStatus.Cancelled;
                loanRepo.Update(loan);

                await notificationService.SendAsync(
                    loan.BorrowerId,
                    NotificationType.LoanCancelled,
                    $"Your loan request for '{loan.Item?.Title}' has been automatically cancelled due to no response.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );

                _logger.LogInformation("Loan {LoanId} auto-cancelled (pending expired).", loan.Id);
            }

            await loanRepo.SaveChangesAsync();
        }
    }
}