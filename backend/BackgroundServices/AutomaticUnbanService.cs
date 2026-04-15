using backend.Interfaces;


namespace backend.BackgroundServices
{
    public class AutomaticUnbanService : BackgroundService
    {
        private readonly ILogger<AutomaticUnbanService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutomaticUnbanService(
            ILogger<AutomaticUnbanService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutomaticUnbanService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutomaticUnbanService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var users = await adminRepo.GetExpiredBannedUsersAsync();

            foreach (var user in users)
            {
                user.IsBanned = false;
                user.BannedAt = null;
                user.BanReason = null;
                user.BannedByAdminId = null;
                user.BanExpiresAt = null;

                user.LockoutEnd = null;
                user.LockoutEnabled = false;

                adminRepo.Update(user);

                if (!string.IsNullOrEmpty(user.Email))
                {
                    await emailService.SendEmailAsync(
                        user.Email,
                        "Account Unbanned",
                        $"<h2>Hello {user.FullName}</h2><p>Your temporary ban has expired. You can log into your account now.</p>"
                    );
                }

                _logger.LogInformation("Auto-unbanned user {UserId}", user.Id);
            }

            await adminRepo.SaveChangesAsync();
        }
    }
}