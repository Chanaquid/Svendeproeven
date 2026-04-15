using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.ProjectionModels
{
    public class UserWithCounts
    {
        public ApplicationUser User { get; set; } = null!;

        //admins if banned/deleted
        public string? BannedByAdminId { get; set; }
        public string? BannedByAdminName { get; set; }
        public string? BannedByAdminAvatarUrl { get; set; }


        public string? DeletedByAdminId { get; set; }
        public string? DeletedByAdminName { get; set; }
        public string? DeletedByAdminAvatarUrl { get; set; }


        public int OwnedItemsCount { get; set; }
        public int BorrowedLoansCount { get; set; }
        public int GivenLoansCount { get; set; }
        public int FinesCount { get; set; }
        public int ScoreHistoryCount { get; set; }
        public int AppealsCount { get; set; }
        public int SupportThreadsCount { get; set; }
        public int ItemReviewsCount { get; set; }
        public int ReviewsGivenCount { get; set; }
        public int ReviewsReceivedCount { get; set; }
        public int VerificationRequestsCount { get; set; }
        public int BanHistoryCount { get; set; }
        public int InitiatedDisputesCount { get; set; }
        public int ReceivedDisputesCount { get; set; }

        //if user is admin
        public int ResolvedDisputesCount { get; set; }
        public int ResolvedAppealsCount { get; set; }
        public int ReviewedVerificationRequestsCount { get; set; }
        public int ClaimedSupportThreadsCount { get; set; }




    }
}
