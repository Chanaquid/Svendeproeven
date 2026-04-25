using backend.BackgroundServices;
using backend.Configuration;
using backend.Data;
using backend.Hubs;
using backend.Interfaces;
using backend.Middleware;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy => policy
        .WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

//Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

//builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
//{
//    options.TokenLifespan = TimeSpan.FromMinutes(15);
//});

//JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/direct-chat") ||
                 path.StartsWithSegments("/hubs/loan-chat") ||
                 path.StartsWithSegments("/hubs/notifications")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };


});

//Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.Configure<ScoreThresholdOptions>(
    builder.Configuration.GetSection(ScoreThresholdOptions.SectionName) );


builder.Services.AddSingleton<IOnlineTracker, OnlineTracker>();

//Background services
builder.Services.AddHostedService<AutomaticUnbanService>();
builder.Services.AddHostedService<AutoCloseInactiveSupportThreadsService>();
builder.Services.AddHostedService<AutoCloseExpiredDisputesService>();
builder.Services.AddHostedService<AutoMarkLoansLateService>();
builder.Services.AddHostedService<AutoExpirePendingLoansService>();

//Services - repo
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IDirectConversationRepository, DirectConversationRepository>();
builder.Services.AddScoped<IAppealRepository, AppealRepository>();
builder.Services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IFineRepository, FineRepository>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<ILoanMessageRepository, LoanMessageRepository>();
builder.Services.AddScoped<IUserReviewRepository, UserReviewRepository>();
builder.Services.AddScoped<IItemReviewRepository, ItemReviewRepository>();
builder.Services.AddScoped<IVerificationRequestRepository, VerificationRequestRepository>();
builder.Services.AddScoped<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddScoped<ISupportRepository, SupportRepository>();
builder.Services.AddScoped<IUserFavoriteRepository, UserFavoriteRepository>();
builder.Services.AddScoped<IUserRecentlyViewedRepository, UserRecentlyViewedRepository>();
builder.Services.AddScoped<IScoreHistoryRepository, ScoreHistoryRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IUserBanHistoryRepository, UserBanHistoryRepository>();

//Services - services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDirectConversationService, DirectConversationService>();
builder.Services.AddScoped<IDirectMessageService, DirectMessageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<IAppealService, AppealService>();
builder.Services.AddScoped<ILoanMessageService, LoanMessageService>();
builder.Services.AddScoped<IItemReviewService, ItemReviewService>();
builder.Services.AddScoped<IUserReviewService, UserReviewService>();
builder.Services.AddScoped<IVerificationRequestService, VerificationRequestService>();
builder.Services.AddScoped<IUserBlockService, UserBlockService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IUserFavoriteService, UserFavoriteService>();
builder.Services.AddScoped<IUserRecentlyViewedService, UserRecentlyViewedService>();
builder.Services.AddScoped<IScoreHistoryService, ScoreHistoryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserBanHistoryService, UserBanHistoryService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<CloudinaryService>();


builder.Services.AddHttpClient();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
    });
builder.Services.AddSignalR();

var app = builder.Build();

//Seed
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Roles
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Default System admin
    var adminEmail = config["Seed:AdminEmail"]!;
    var adminPassword = config["Seed:AdminPassword"]!;

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "SYSTEM_ADMIN",
            Email = adminEmail,
            FullName = "SystemAdmin",
            Address = "Copenhagen",
            Gender = "Male",
            Bio = "This is a systemgenerated root Admin.",
            Latitude = 55.67594,
            Longitude = 12.56553,
            DateOfBirth = new DateTime(1990, 1, 1),
            EmailConfirmed = true,
            IsVerified = true,
            Score = 100,
            MembershipDate = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Default categories
    var defaultCategories = new[]
    {
        ("Electronics", "📱", "electronics"),
        ("Tools", "🔧", "tools"),
        ("Sports", "⚽", "sports"),
        ("Music", "🎸", "music"),
        ("Books", "📚", "books"),
        ("Camping", "⛺", "camping"),
        ("Photography", "📷", "photography"),
        ("Gaming", "🎮", "gaming"),
        ("Gardening", "🌱", "gardening"),
        ("Biking", "🚲", "biking"),
        ("Kitchen", "🍳", "kitchen"),
        ("Cleaning", "🧹", "cleaning"),
        ("Fashion", "👗", "fashion"),
        ("Art", "🎨", "art"),
        ("Baby", "👶", "baby"),
        ("Events", "🎉", "events"),
        ("Auto", "🚗", "auto"),
        ("Other", "📦", "other")
    };

    foreach (var (name, icon, slug) in defaultCategories)
    {
        if (!context.Categories.Any(c => c.Name == name))
        {
            context.Categories.Add(new Category
            {
                Name = name,
                Icon = icon,
                Slug = slug
            });
        }
    }

    await context.SaveChangesAsync();
}

//Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
//SignalR
app.MapControllers();
app.MapHub<DirectChatHub>("/hubs/direct-chat");
app.MapHub<LoanChatHub>("/hubs/loan-chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();