using backend.Interfaces;

namespace backend.BackgroundServices
{
    public class AutoCloseInactiveSupportThreadsService : BackgroundService
    {
        private readonly ILogger<AutoCloseInactiveSupportThreadsService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutoCloseInactiveSupportThreadsService(
            ILogger<AutoCloseInactiveSupportThreadsService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCloseInactiveSupportThreadsService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoCloseInactiveSupportThreadsService");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var supportService = scope.ServiceProvider.GetRequiredService<ISupportService>();

            await supportService.AutoCloseInactiveThreadsAsync();

            _logger.LogInformation("AutoCloseInactiveSupportThreadsService: processed inactive threads.");
        }
    }
}