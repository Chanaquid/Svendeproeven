using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        //Personal Infos
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Gender { get; set; }

        public string? Bio { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime DateOfBirth { get; set; }

        //Calculated property so we don't have to store Age in the DB
        [NotMapped]
        public int Age
        {
            get
            {
                var today = DateTime.UtcNow.Date;
                int age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public bool IsVerified { get; set; } = false; //IS VERIFIED??? real identity? or maybe scammer?

        public DateTime? PhoneNumberVerifiedAt { get; set; } //phone verified separately from email

        public string? AvatarUrl { get; set; }

        public DateTime MembershipDate { get; set; } = DateTime.UtcNow;

        public int Score { get; set; } = 100;
        public DateTime? LastScoreAppealRejectedAt { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        //baned?
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BanExpiresAt { get; set; } //For temporary bans
        public string? BannedByAdminId { get; set; }
        public ApplicationUser? BannedByAdmin {  get; set; }

        //Soft delete
        //When user is deleted, their username and email is freed so they can create another acc with the same email and username
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByAdminId { get; set; }
        public string? DeletionNote { get; set; }
        public ApplicationUser? DeletedByAdmin { get; set; }
        //Refresh tokens
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        //Navigations
        public ICollection<Item> OwnedItems { get; set; } = new List<Item>();
        public ICollection<Loan> BorrowedLoans { get; set; } = new List<Loan>();
        public ICollection<Loan> GivenLoans { get; set; } = new List<Loan>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>();
        public ICollection<ScoreHistory> ScoreHistory { get; set; } = new List<ScoreHistory>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
        public ICollection<SupportThread> SupportThreads { get; set; } = new List<SupportThread>(); 
        public ICollection<UserFavoriteItem> FavoriteItems { get; set; } = new List<UserFavoriteItem>();
        public ICollection<UserRecentlyViewedItem> RecentlyViewed { get; set; } = new List<UserRecentlyViewedItem>();
        public ICollection<ItemReview> ItemReviews { get; set; } = new List<ItemReview>();
        public ICollection<UserReview> ReviewsGiven { get; set; } = new List<UserReview>(); //Reviews this user wrote
        public ICollection<UserReview> ReviewsReceived { get; set; } = new List<UserReview>();  //Reviews written about this user
        public ICollection<VerificationRequest> VerificationRequests { get; set; } = new List<VerificationRequest>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>(); //Blocks this user has placed
        public ICollection<UserBlock> BlockedBy { get; set; } = new List<UserBlock>(); //All users that has blocked this user
        public ICollection<UserBanHistory> BanHistory { get; set; } = new List<UserBanHistory>();
        public ICollection<DirectConversation> InitiatedConversations { get; set; } = new List<DirectConversation>(); //Conversations initiated
        public ICollection<DirectConversation> ReceivedConversations { get; set; } = new List<DirectConversation>(); //Conversations where user is the other participant

        
        public ICollection<Dispute> InitiatedDisputes { get; set; } = new List<Dispute>();      
        public ICollection<Dispute> ReceivedDisputes { get; set; } = new List<Dispute>();  
        public ICollection<DisputePhoto> SubmittedDisputePhotos { get; set; } = new List<DisputePhoto>();



        //For admin users
        public ICollection<Dispute> ResolvedDisputes { get; set; } = new List<Dispute>();
        public ICollection<Appeal> ResolvedAppeals { get; set; } = new List<Appeal>();
        public ICollection<VerificationRequest> ReviewedVerificationRequests { get; set; } = new List<VerificationRequest>();

        public ICollection<SupportThread> ClaimedSupportThreads { get; set; } = new List<SupportThread>(); 


    }
}
