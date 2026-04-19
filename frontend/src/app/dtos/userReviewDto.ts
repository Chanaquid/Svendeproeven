export interface CreateUserReviewDto {
  loanId: number;
  rating: number;
  comment?: string | null;
}

export interface UpdateUserReviewDto {
  rating: number;
  comment?: string | null;
}

export interface AdminCreateUserReviewDto {
  reviewedUserId: string;
  rating: number;
  comment: string;
}

export interface UserReviewDto {
  id: number;
  loanId: number | null;
  reviewerId: string;
  reviewerName: string;
  reviewerUserName: string;
  reviewerAvatarUrl: string | null;
  reviewedUserId: string;
  reviewedFullName: string;
  reviewedUserName: string;
  reviewedUserAvatarUrl: string | null;
  isMine: boolean;
  isAdminReview: boolean;
  isEdited: boolean;
  editedAt: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface UserReviewListDto {
  id: number;
  reviewerId: string;
  reviewerName: string;
  reviewerUserName: string;
  reviewerAvatarUrl: string | null;
  itemTitle : string | null;
  isAdminReview: boolean;
  isMine: boolean;
  isEdited: boolean;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface UserRatingSummaryDto {
  averageRating: number;
  totalReviews: number;
  rating1Count: number;
  rating2Count: number;
  rating3Count: number;
  rating4Count: number;
  rating5Count: number;
}