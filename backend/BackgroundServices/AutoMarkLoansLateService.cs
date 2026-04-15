using backend.Interfaces;
using backend.Models;

namespace backend.BackgroundServices
{
    public class AutoMarkLoansLateService : BackgroundService
    {
        private readonly ILogger<AutoMarkLoansLateService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutoMarkLoansLateService(
            ILogger<AutoMarkLoansLateService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoMarkLoansLateService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoMarkLoansLateService");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task ProcessAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var _loanRepository = scope.ServiceProvider.GetRequiredService<ILoanRepository>();
            var scoreHistoryRepo = scope.ServiceProvider.GetRequiredService<IScoreHistoryRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Active loans past their EndDate with no return
            var overdueLoans = await _loanRepository.GetOverdueActiveLoansAsync();

            foreach (var loan in overdueLoans)
            {
                loan.Status = LoanStatus.Late;
                _loanRepository.Update(loan);

                //-5 per day late, max -15 per loan, floor 0 — first day penalty
                var daysLate = (int)(DateTime.UtcNow.Date - loan.EndDate.Date).TotalDays;
                var pointsToDeduct = Math.Min(daysLate * 5, 15);
                var newScore = Math.Max(loan.Borrower.Score - pointsToDeduct, 0);
                var actualPointsChanged = newScore - loan.Borrower.Score;

                if (actualPointsChanged != 0)
                {
                    loan.Borrower.Score = newScore;

                    await scoreHistoryRepo.AddAsync(new ScoreHistory
                    {
                        UserId = loan.BorrowerId,
                        LoanId = loan.Id,
                        PointsChanged = actualPointsChanged,
                        ScoreAfterChange = newScore,
                        Reason = ScoreChangeReason.LateReturn,
                        Note = $"Loan {loan.Id} overdue by {daysLate} day(s).",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await notificationService.SendAsync(
                    loan.BorrowerId,
                    NotificationType.LoanOverdue,
                    $"Your loan for '{loan.Item?.Title}' is overdue. Please return it as soon as possible.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );

                await notificationService.SendAsync(
                    loan.LenderId,
                    NotificationType.LoanOverdue,
                    $"The borrower has not returned '{loan.Item?.Title}' on time.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );

                _logger.LogInformation("Loan {LoanId} marked as Late.", loan.Id);
            }

            await _loanRepository.SaveChangesAsync();
            await scoreHistoryRepo.SaveChangesAsync();
        }
    }
}