using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemPhoto> ItemPhotos { get; set; }
        public DbSet<ItemReview> ItemReviews { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanMessage> LoanMessages { get; set; }
        public DbSet<LoanSnapshotPhoto> LoanSnapshotPhotos { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<DisputePhoto> DisputePhotos { get; set; }
        public DbSet<Appeal> Appeals { get; set; }
        public DbSet<ScoreHistory> ScoreHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<SupportThread> SupportThreads { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<VerificationRequest> VerificationRequests { get; set; }
        public DbSet<UserFavoriteItem> UserFavoriteItems { get; set; }
        public DbSet<UserRecentlyViewedItem> UserRecentlyViewedItems { get; set; }
        public DbSet<UserReview> UserReviews { get; set; }
        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<UserBanHistory> UserBanHistories { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            //ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.HasIndex(u => u.IsDeleted);
                entity.HasIndex(u => u.IsBanned);
                entity.HasIndex(u => u.Score);
                entity.HasIndex(u => u.IsVerified);
                entity.HasIndex(u => new { u.Latitude, u.Longitude });
                entity.HasIndex(u => u.CreatedAt);
                entity.HasIndex(u => new { u.IsDeleted, u.IsBanned });
                // Index for score appeal cooldown lookups
                entity.HasIndex(u => u.LastScoreAppealRejectedAt);

                entity.HasOne(u => u.BannedByAdmin)
                    .WithMany()
                    .HasForeignKey(u => u.BannedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.DeletedByAdmin)
                    .WithMany()
                    .HasForeignKey(u => u.DeletedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasIndex(c => c.Name).IsUnique();
                entity.HasIndex(c => c.Slug).IsUnique();
                entity.HasIndex(c => c.IsActive);
                entity.HasIndex(c => new { c.IsActive, c.Name });

                entity.HasMany(c => c.Items)
                    .WithOne(i => i.Category)
                    .HasForeignKey(i => i.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Item
            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.RowVersion).IsRowVersion();

                entity.HasIndex(i => i.OwnerId);
                entity.HasIndex(i => i.CategoryId);
                entity.HasIndex(i => i.Status);
                entity.HasIndex(i => i.Availability);
                entity.HasIndex(i => i.IsActive);
                entity.HasIndex(i => i.QrCode).IsUnique();
                entity.HasIndex(i => i.Slug).IsUnique();
                entity.HasIndex(i => new { i.AvailableFrom, i.AvailableUntil });
                entity.HasIndex(i => i.CreatedAt);
                entity.HasIndex(i => new { i.Status, i.Availability, i.IsActive });
                entity.HasIndex(i => new { i.Status, i.IsActive, i.CategoryId });
                entity.HasIndex(i => new { i.PickupLatitude, i.PickupLongitude });
                entity.HasIndex(i => i.IsFree);
                entity.HasIndex(i => i.RequiresVerification);
                entity.HasIndex(i => new { i.OwnerId, i.IsActive, i.Status });

                entity.Property(i => i.CurrentValue).HasPrecision(18, 2);
                entity.Property(i => i.PricePerDay).HasPrecision(18, 2);
                entity.Property(i => i.AverageRating).HasPrecision(4, 2);


                entity.HasQueryFilter(i => !i.IsDeleted);

                entity.HasOne(i => i.Owner)
                    .WithMany(u => u.OwnedItems)
                    .HasForeignKey(i => i.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Category)
                    .WithMany(c => c.Items)
                    .HasForeignKey(i => i.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.ReviewedByAdmin)
                    .WithMany()
                    .HasForeignKey(i => i.ReviewedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                //DeletedBy — who soft-deleted this item
                entity.HasOne(i => i.DeletedBy)
                    .WithMany()
                    .HasForeignKey(i => i.DeletedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                //ItemPhotos are the ONLY model that can be hard-deleted (Cascade)
                entity.HasMany(i => i.Photos)
                    .WithOne(p => p.Item)
                    .HasForeignKey(p => p.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(i => i.Loans)
                    .WithOne(l => l.Item)
                    .HasForeignKey(l => l.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(i => i.Reviews)
                    .WithOne(r => r.Item)
                    .HasForeignKey(r => r.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasCheckConstraint("CK_Item_Price_NonNegative", "[PricePerDay] >= 0");
            });

            // ItemPhoto
            modelBuilder.Entity<ItemPhoto>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasIndex(p => p.ItemId);
                entity.HasIndex(p => new { p.ItemId, p.IsPrimary });
                entity.HasIndex(p => new { p.ItemId, p.DisplayOrder });

                // Hard delete allowed — no soft delete on ItemPhoto
                entity.HasOne(p => p.Item)
                    .WithMany(i => i.Photos)
                    .HasForeignKey(p => p.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ItemReview
            modelBuilder.Entity<ItemReview>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => r.ItemId);
                entity.HasIndex(r => r.ReviewerId);
                //One review per loan (unique, nullable FK)
                entity.HasIndex(r => r.LoanId).IsUnique().HasFilter("[LoanId] IS NOT NULL");
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => new { r.ItemId, r.Rating });
                entity.HasIndex(r => r.IsAdminReview);

                entity.HasQueryFilter(r => !r.IsDeleted);

                entity.HasOne(r => r.Item)
                    .WithMany(i => i.Reviews)
                    .HasForeignKey(r => r.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewer)
                    .WithMany(u => u.ItemReviews)
                    .HasForeignKey(r => r.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Loan)
                    .WithMany()
                    .HasForeignKey(r => r.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Loan
            modelBuilder.Entity<Loan>(entity =>
            {
                entity.HasKey(l => l.Id);

                entity.HasIndex(l => l.ItemId);
                entity.HasIndex(l => l.BorrowerId);
                entity.HasIndex(l => l.LenderId);
                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => new { l.StartDate, l.EndDate });
                entity.HasIndex(l => l.CreatedAt);
                entity.HasIndex(l => new { l.Status, l.BorrowerId });
                entity.HasIndex(l => new { l.Status, l.ItemId });
                entity.HasIndex(l => new { l.EndDate, l.Status });
                entity.HasIndex(l => l.AdminReviewerId);
                entity.HasIndex(l => l.OwnerApproverId);
                entity.HasIndex(l => l.ExtensionRequestStatus);
                entity.HasIndex(l => l.PickedUpAt);
                entity.HasIndex(l => l.ReturnedAt);
                //For dispute deadline enforcement
                entity.HasIndex(l => l.DisputeDeadline);
                entity.HasIndex(l => new { l.Status, l.DisputeDeadline });

                entity.Property(l => l.PricePerDaySnapshot).HasPrecision(18, 2);
                entity.Property(l => l.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(l => l.Item)
                    .WithMany(i => i.Loans)
                    .HasForeignKey(l => l.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Borrower)
                    .WithMany(u => u.BorrowedLoans)
                    .HasForeignKey(l => l.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Lender)
                    .WithMany(u => u.GivenLoans)
                    .HasForeignKey(l => l.LenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.AdminReviewer)
                    .WithMany()
                    .HasForeignKey(l => l.AdminReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.OwnerApprover)
                    .WithMany()
                    .HasForeignKey(l => l.OwnerApproverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(l => l.SnapshotPhotos)
                    .WithOne(s => s.Loan)
                    .HasForeignKey(s => s.LoanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(l => l.Fines)
                    .WithOne(f => f.Loan)
                    .HasForeignKey(f => f.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(l => l.Messages)
                    .WithOne(m => m.Loan)
                    .HasForeignKey(m => m.LoanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(l => l.Disputes)
                    .WithOne(d => d.Loan)
                    .HasForeignKey(d => d.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // LoanMessage
            modelBuilder.Entity<LoanMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasIndex(m => m.LoanId);
                entity.HasIndex(m => m.SenderId);
                entity.HasIndex(m => new { m.LoanId, m.IsRead });
                entity.HasIndex(m => m.SentAt);

                entity.HasOne(m => m.Loan)
                    .WithMany(l => l.Messages)
                    .HasForeignKey(m => m.LoanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // LoanSnapshotPhoto
            modelBuilder.Entity<LoanSnapshotPhoto>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => s.LoanId);
                entity.HasIndex(s => new { s.LoanId, s.DisplayOrder });

                entity.HasOne(s => s.Loan)
                    .WithMany(l => l.SnapshotPhotos)
                    .HasForeignKey(s => s.LoanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            
            //Fine
            modelBuilder.Entity<Fine>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.HasIndex(f => f.UserId);
                entity.HasIndex(f => f.LoanId);
                entity.HasIndex(f => f.DisputeId);
                entity.HasIndex(f => f.Status);
                entity.HasIndex(f => f.Type);
                entity.HasIndex(f => new { f.UserId, f.Status });
                entity.HasIndex(f => f.CreatedAt);
                entity.HasIndex(f => f.IssuedByAdminId);
                entity.HasIndex(f => f.VerifiedByAdminId);
                entity.HasIndex(f => new { f.LoanId, f.DisputeId, f.Status });

                entity.Property(f => f.Amount).HasPrecision(18, 2);
                entity.Property(f => f.ItemValueAtTimeOfFine).HasPrecision(18, 2);

                entity.HasOne(f => f.User)
                    .WithMany(u => u.Fines)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Loan)
                    .WithMany(l => l.Fines)
                    .HasForeignKey(f => f.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Dispute)
                    .WithMany(d => d.Fines)
                    .HasForeignKey(f => f.DisputeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.IssuedByAdmin)
                    .WithMany()
                    .HasForeignKey(f => f.IssuedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.VerifiedByAdmin)
                    .WithMany()
                    .HasForeignKey(f => f.VerifiedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                //Fine has exactly one optional Appeal (Appeal owns the FK)
                entity.HasOne(f => f.Appeal)
                    .WithOne(a => a.Fine)
                    .HasForeignKey<Appeal>(a => a.FineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            
            //Dispute
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.HasIndex(d => d.LoanId);
                entity.HasIndex(d => d.FiledById);
                entity.HasIndex(d => d.RespondedById);
                entity.HasIndex(d => d.Status);
                entity.HasIndex(d => d.ResolvedByAdminId);
                entity.HasIndex(d => d.CreatedAt);
                entity.HasIndex(d => new { d.Status, d.ResponseDeadline });
                entity.HasIndex(d => d.FiledAs);

                //Enforce max 1 dispute per party per loan:
                entity.HasIndex(d => new { d.LoanId, d.FiledAs }).IsUnique();

                entity.Property(d => d.CustomFineAmount).HasPrecision(18, 2);

                entity.HasOne(d => d.Loan)
                    .WithMany(l => l.Disputes)
                    .HasForeignKey(d => d.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.FiledBy)
                    .WithMany(u => u.InitiatedDisputes)
                    .HasForeignKey(d => d.FiledById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.RespondedBy)
                    .WithMany(u => u.ReceivedDisputes)
                    .HasForeignKey(d => d.RespondedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ResolvedByAdmin)
                    .WithMany(u => u.ResolvedDisputes)
                    .HasForeignKey(d => d.ResolvedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Photos)
                    .WithOne(p => p.Dispute)
                    .HasForeignKey(p => p.DisputeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(d => d.Fines)
                    .WithOne(f => f.Dispute)
                    .HasForeignKey(f => f.DisputeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            
            // DisputePhoto
            modelBuilder.Entity<DisputePhoto>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasIndex(p => p.DisputeId);
                entity.HasIndex(p => p.SubmittedById);
                entity.HasIndex(p => p.UploadedAt);

                entity.HasOne(p => p.Dispute)
                    .WithMany(d => d.Photos)
                    .HasForeignKey(p => p.DisputeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.SubmittedBy)
                    .WithMany(u => u.SubmittedDisputePhotos)
                    .HasForeignKey(p => p.SubmittedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Appeal
            modelBuilder.Entity<Appeal>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.FineId);
                entity.HasIndex(a => a.ScoreHistoryId);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => a.ResolvedByAdminId);
                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => new { a.AppealType, a.Status });
                //For "one pending score appeal per user" enforcement
                entity.HasIndex(a => new { a.UserId, a.AppealType, a.Status });

                entity.HasQueryFilter(a => !a.IsDeleted);

                entity.Property(a => a.CustomFineAmount).HasPrecision(18, 2);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.Appeals)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ScoreHistory)
                    .WithMany()
                    .HasForeignKey(a => a.ScoreHistoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ResolvedByAdmin)
                    .WithMany(u => u.ResolvedAppeals)
                    .HasForeignKey(a => a.ResolvedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            
            //ScoreHistory
            modelBuilder.Entity<ScoreHistory>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.LoanId);
                entity.HasIndex(s => s.DisputeId);
                entity.HasIndex(s => s.CreatedAt);
                entity.HasIndex(s => new { s.UserId, s.CreatedAt });
                entity.HasIndex(s => s.Reason);

                entity.HasOne(s => s.User)
                    .WithMany(u => u.ScoreHistory)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Loan)
                    .WithMany()
                    .HasForeignKey(s => s.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Dispute)
                    .WithMany()
                    .HasForeignKey(s => s.DisputeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => n.CreatedAt);
                entity.HasIndex(n => new { n.ReferenceType, n.ReferenceId });
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => new { n.UserId, n.CreatedAt });

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //DirectConversation
            modelBuilder.Entity<DirectConversation>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasIndex(c => c.InitiatedById);
                entity.HasIndex(c => c.OtherUserId);
                entity.HasIndex(c => new { c.InitiatedById, c.OtherUserId }).IsUnique();
                entity.HasIndex(c => new { c.InitiatedById, c.HiddenForInitiator });
                entity.HasIndex(c => new { c.OtherUserId, c.HiddenForOther });
                entity.HasIndex(c => c.LastMessageAt);
                entity.HasIndex(c => c.CreatedAt);

                entity.HasOne(c => c.InitiatedBy)
                    .WithMany(u => u.InitiatedConversations)
                    .HasForeignKey(c => c.InitiatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.OtherUser)
                    .WithMany(u => u.ReceivedConversations)
                    .HasForeignKey(c => c.OtherUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Messages)
                    .WithOne(m => m.Conversation)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.LastMessage)
                    .WithMany()
                    .HasForeignKey(c => c.LastMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasCheckConstraint("CK_DirectConversation_DifferentUsers", "[InitiatedById] != [OtherUserId]");
            });

            // DirectMessage
            modelBuilder.Entity<DirectMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasIndex(m => m.ConversationId);
                entity.HasIndex(m => m.SenderId);
                entity.HasIndex(m => new { m.ConversationId, m.IsRead });
                entity.HasIndex(m => m.SentAt);

                entity.Property(m => m.Content).HasMaxLength(2000);

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SupportThread
            modelBuilder.Entity<SupportThread>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.ClaimedByAdminId);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => new { t.Status, t.ClaimedByAdminId });
                entity.HasIndex(t => t.CreatedAt);

                entity.HasOne(t => t.User)
                    .WithMany(u => u.SupportThreads)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ClaimedByAdmin)
                    .WithMany(u => u.ClaimedSupportThreads)
                    .HasForeignKey(t => t.ClaimedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(t => t.Messages)
                    .WithOne(m => m.SupportThread)
                    .HasForeignKey(m => m.SupportThreadId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //SupportMessage
            modelBuilder.Entity<SupportMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasIndex(m => m.SupportThreadId);
                entity.HasIndex(m => m.SenderId);
                entity.HasIndex(m => new { m.SupportThreadId, m.IsRead });
                entity.HasIndex(m => m.SentAt);

                entity.HasOne(m => m.SupportThread)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(m => m.SupportThreadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            
            //VerificationRequest
            modelBuilder.Entity<VerificationRequest>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasIndex(v => v.UserId);
                entity.HasIndex(v => v.Status);
                entity.HasIndex(v => v.ReviewedByAdminId);
                entity.HasIndex(v => v.SubmittedAt);
                entity.HasIndex(v => new { v.Status, v.SubmittedAt });
                entity.HasIndex(v => v.DocumentType);
                //For "only one pending verification per user" enforcement
                entity.HasIndex(v => new { v.UserId, v.Status });

                entity.HasOne(v => v.User)
                    .WithMany(u => u.VerificationRequests)
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.ReviewedByAdmin)
                    .WithMany(u => u.ReviewedVerificationRequests)
                    .HasForeignKey(v => v.ReviewedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            
            //UserFavoriteItem
            modelBuilder.Entity<UserFavoriteItem>(entity =>
            {
                entity.HasKey(f => new { f.UserId, f.ItemId });

                entity.HasIndex(f => f.UserId);
                entity.HasIndex(f => f.ItemId);
                entity.HasIndex(f => f.SavedAt);
                entity.HasIndex(f => new { f.UserId, f.NotifyWhenAvailable });

                entity.HasOne(f => f.User)
                    .WithMany(u => u.FavoriteItems)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Item)
                    .WithMany(i => i.FavoritedBy)
                    .HasForeignKey(f => f.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //UserRecentlyViewedItem
            modelBuilder.Entity<UserRecentlyViewedItem>(entity =>
            {
                entity.HasKey(v => new { v.UserId, v.ItemId });

                entity.HasIndex(v => v.UserId);
                entity.HasIndex(v => new { v.UserId, v.ViewedAt });

                entity.HasOne(v => v.User)
                    .WithMany(u => u.RecentlyViewed)
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.Item)
                    .WithMany(i => i.RecentlyViewedBy)
                    .HasForeignKey(v => v.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            //UserReview
            modelBuilder.Entity<UserReview>(entity =>
            {
                entity.HasKey(r => r.Id);

                //One review per loan per reviewer (unique, nullable FK)
                entity.HasIndex(r => new { r.LoanId, r.ReviewerId })
                          .IsUnique()
                          .HasFilter("[LoanId] IS NOT NULL"); 
                entity.HasIndex(r => r.ReviewerId);
                entity.HasIndex(r => r.ReviewedUserId);
                entity.HasIndex(r => new { r.ReviewedUserId, r.Rating });
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => r.IsAdminReview);
                entity.HasIndex(r => new { r.ReviewerId, r.ReviewedUserId });
                entity.HasIndex(r => r.IsDeleted);

                entity.HasQueryFilter(r => !r.IsDeleted);


                entity.HasOne(r => r.Loan)
                    .WithMany()
                    .HasForeignKey(r => r.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewer)
                    .WithMany(u => u.ReviewsGiven)
                    .HasForeignKey(r => r.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReviewedUser)
                    .WithMany(u => u.ReviewsReceived)
                    .HasForeignKey(r => r.ReviewedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasCheckConstraint("CK_UserReview_NoSelfReview", "[ReviewerId] != [ReviewedUserId]");
            });

            //UserBlock
            modelBuilder.Entity<UserBlock>(entity =>
            {
                entity.HasKey(b => new { b.BlockerId, b.BlockedId });

                entity.HasIndex(b => b.BlockerId);
                entity.HasIndex(b => b.BlockedId);
                entity.HasIndex(b => b.CreatedAt);

                entity.HasOne(b => b.Blocker)
                    .WithMany(u => u.BlockedUsers)
                    .HasForeignKey(b => b.BlockerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Blocked)
                    .WithMany(u => u.BlockedBy)
                    .HasForeignKey(b => b.BlockedId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasCheckConstraint("CK_UserBlock_NoSelfBlock", "[BlockerId] != [BlockedId]");
            });

            //UserBanHistory
            modelBuilder.Entity<UserBanHistory>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.AdminId);
                entity.HasIndex(b => b.BannedAt);
                entity.HasIndex(b => b.IsBanned);
                entity.HasIndex(b => new { b.UserId, b.BannedAt });
                entity.HasIndex(b => b.BanExpiresAt);

                entity.HasOne(b => b.User)
                    .WithMany(u => u.BanHistory)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Admin)
                    .WithMany()
                    .HasForeignKey(b => b.AdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Report
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => r.ReportedById);
                entity.HasIndex(r => r.Type);
                entity.HasIndex(r => r.TargetId);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.HandledByAdminId);
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => new { r.Status, r.Type });
                entity.HasIndex(r => new { r.Type, r.TargetId });

                entity.HasOne(r => r.ReportedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ReportedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.HandledByAdmin)
                    .WithMany()
                    .HasForeignKey(r => r.HandledByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}