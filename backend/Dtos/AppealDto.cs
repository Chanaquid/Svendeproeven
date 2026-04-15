using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //User files a score appeal when their score drops below 20
    public class CreateScoreAppealDto
    {

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    //User submits a fine appeal
    public class CreateFineAppealDto
    {

        [Required]
        public int FineId { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    //Admin approves or rejects the score appeal
    public class AdminDecidesScoreAppealDto
    {
        [Required]
        public bool IsApproved { get; set; }
        public string? AdminNote { get; set; }
        public int? NewScore { get; set; } //Defaults to 20 if not set
    }

    //Admin decides a fine appeal
    public class AdminDecidesFineAppealDto
    {
        [Required]
        public bool IsApproved { get; set; }
        public string? AdminNote { get; set; }
        public FineAppealResolution? Resolution { get; set; } //Required if approved
        public decimal? CustomFineAmount { get; set; } //Required if Resolution = Custom
    }

    public class AppealDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName {  get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }

        public AppealType AppealType { get; set; }
        public string Message { get; set; } = string.Empty;
        public AppealStatus Status { get; set; }


        //fine
        public int? FineId { get; set; }
        public decimal? FineAmount { get; set; }
        public FineAppealResolution? FineResolution { get; set; }
        public decimal? CustomFineAmount { get; set; }


        public int? ScoreHistoryId { get; set; }
        public int? RestoredScore { get; set; }
        public int? ScoreAfterChange { get; set; }


        public string? ResolvedByAdminId { get; set; }
        public string? ResolvedByAdminName { get; set; }
        public string? ResolvedByAdminUserName { get; set; }
        public string? ResolvedByAdminAvatarUrl { get; set; }
        public string? AdminNote { get; set; }


        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    //For grid view 
    public class AppealListDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName {  get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public AppealType AppealType { get; set; }
        public AppealStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class AdminAppealDto : AppealDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public int UserCurrentScore { get; set; }
        public decimal UnpaidFinesTotal { get; set; }

        //User History
        public DateTime MembershipDate { get; set; }
        public int SuccessfulBorrowCount { get; set; }
        public int SuccessfulLendCount { get; set; }

        public double? HoursToResolve { get; set; }

    }



}