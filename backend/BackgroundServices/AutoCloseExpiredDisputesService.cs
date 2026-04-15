using backend.Interfaces;
using backend.Models;

namespace backend.BackgroundServices
{
    public class AutoCloseExpiredDisputesService : BackgroundService
    {
        private readonly ILogger<AutoCloseExpiredDisputesService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutoCloseExpiredDisputesService(
            ILogger<AutoCloseExpiredDisputesService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCloseExpiredDisputesService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoCloseExpiredDisputesService");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var disputeRepo = scope.ServiceProvider.GetRequiredService<IDisputeRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Disputes still AwaitingResponse past their ResponseDeadline
            var expiredDisputes = await disputeRepo.GetExpiredAwaitingResponseAsync();

            foreach (var dispute in expiredDisputes)
            {
                dispute.Status = DisputeStatus.PastDeadline;
                disputeRepo.Update(dispute);

                // Notify both parties
                await notificationService.SendAsync(
                    dispute.FiledById,
                    NotificationType.DisputeExpired,
                    "Your dispute has passed the response deadline and has been marked as past deadline.",
                    dispute.Id,
                    NotificationReferenceType.Dispute
                );

                if (!string.IsNullOrEmpty(dispute.RespondedById))
                {
                    await notificationService.SendAsync(
                        dispute.RespondedById,
                        NotificationType.DisputeExpired,
                        "A dispute filed against you has passed the response deadline.",
                        dispute.Id,
                        NotificationReferenceType.Dispute
                    );
                }

                _logger.LogInformation("Dispute {DisputeId} marked as PastDeadline.", dispute.Id);
            }

            await disputeRepo.SaveChangesAsync();
        }
    }
}