using backend.Models;
using System.ComponentModel.DataAnnotations;
namespace backend.Dtos
{

    //Admin manually adjusts a user's score
    public class AdminScoreAdjustDto
    {
        [Required]
        public int PointsChanged { get; set; } //Signed: +10 or -10

        [Required, MaxLength(500)]
        public string Note { get; set; } = string.Empty;
    }

    //Admin edits a user's profile and account fields
    public class AdminEditUserDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? NewPassword { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool? IsVerified { get; set; }
        public string? Role { get; set; } //"User" or "Admin"
        public int? Score { get; set; }
        public string? ScoreNote { get; set; } //Required if Score is set

    }

    public class AdminDeleteResultDto
    {
        public bool Success { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    //Admin view of a user
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber {  get; set; }

        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? AvatarUrl { get; set; }
        public int Age { get; set; }
        public string Role { get; set; } = string.Empty;


        public int Score { get; set; }
        public bool IsVerified { get; set; }
        public DateTime MembershipDate { get; set; }
        public decimal UnpaidFinesTotal { get; set; }


        public bool IsBanned { get; set; }
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BanExpiresAt { get; set; }
        public string? BannedByAdminId { get; set; }
        public string? BannedByAdminName { get; set; }
        public string? BannedByAdminAvatarUrl { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByAdminId { get; set; }
        public string? DeletedByAdminName { get; set; }
        public string? DeletedByAdminAvatarUrl { get; set; }
        public string? DeletionNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool EmailConfirmed { get; set; }


        //Counts
        public int TotalOwnedItems { get; set; }     
        public int TotalBorrowedLoans { get; set; }    
        public int TotalGivenLoans { get; set; }        
        public int TotalFines { get; set; }
        public int TotalScoreHistory { get; set; }
        public int TotalAppeals { get; set; }
        public int TotalSupportThreads { get; set; }
        public int TotalItemReviews { get; set; }
        public int TotalReviewsGiven { get; set; }      
        public int TotalReviewsReceived { get; set; }  
        public int TotalVerificationRequests { get; set; }
        public int TotalBanHistory { get; set; }
        public int TotalInitiatedDisputes { get; set; }
        public int TotalReceivedDisputes { get; set; }


        public int TotalDisputesResolved { get; set; } 
        public int TotalAppealsResolved { get; set; }   
        public int TotalVerificationRequestsReviewed { get; set; } 
        public int TotalSupportThreadsClaimed { get; set; } 


    }

    public class UserListForAdminsDto : UserListForUsersDto
        {

            public string? Email { get; set; }
            public string? Gender { get; set; }
            public string Role { get; set; } = string.Empty;
            public string? Bio { get; set; }

            public int TotalItems { get; set; }
            public int BorrowedLoansCount { get; set; }
            public int GivenLoansCount { get; set; }

            //Not full address
            public string? GeneralAddress { get; set; }
            public int TotalReviewsReceived { get; set; }

        }

    
}
