using backend.Models;
using System.ComponentModel.DataAnnotations;


namespace backend.Dtos
{

    public class RegisterUserRequestDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class RegisterUserResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = "Registration successful. Please verify your email.";
    }


    //User updates their own profile
    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? UserName { get; set; }

        [MaxLength(254)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }

    //If frontend has uploadthing setup for drag and drop --> imp
    public class UpdateAvatarDto
    {
        [Required]
        public string AvatarUrl { get; set; } = string.Empty;
    }


    //User deletes their own account — password confirmation required
    public class DeleteAccountDto
    {
        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }

     

    //User's own profile view
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string Role {  get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string? AvatarUrl { get; set; }
        public int Score { get; set; }
        public decimal UnpaidFinesTotal { get; set; }
        public bool IsVerified { get; set; }
        public bool IsBanned { get; set; }
        public int TotalCompletedLoans { get; set; }
        public BorrowingStatus BorrowingStatus { get; set; } 
        public DateTime MembershipDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }



    //----------------for listings or public profile-----------------
    public class UserListForUsersDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } 
        public int Score { get; set; }
        public bool IsVerified { get; set; }

        public int TotalItems { get; set; }  //How many items they have
        public double? AverageRating { get; set; }  //User rating

        public DateTime MembershipDate { get; set; }

    }


    //User public profile
    public class UserPublicProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Gender { get; set; }
        public int Age { get; set; }
        public bool IsVerified { get; set; }
        public int Score { get; set; }
        public DateTime MembershipDate { get; set; }
        public string? GeneralAddress { get; set; } //NOt full address
        public int TotalCompletedLoans { get; set; }

        public int TotalItems { get; set; }
        public int TotalReviewsReceived { get; set; }

    }
    
}