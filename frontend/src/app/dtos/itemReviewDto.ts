export interface CreateItemReviewDto {
  itemId: number;
  loanId?: number | null;
  rating: number;
  comment?: string | null;
}

export interface UpdateItemReviewDto {
  reviewId: number;
  rating: number;
  comment?: string | null;
}

export interface AdminCreateItemReviewDto {
  itemId: number;
  rating: number;
  comment?: string | null;
}

export interface ItemReviewDto {
  id: number;
  itemId: number;
  loanId: number | null;
  reviewerId: string;
  reviewerName: string;
  reviewerUserName: string;
  reviewerAvatarUrl: string | null;
  isMine: boolean;
  rating: number;
  comment: string | null;
  isAdminReview: boolean;
  isEdited: boolean;
  editedAt: string | null;
  createdAt: string;
}

export interface ItemReviewListDto {
  id: number;
  rating: number;
  comment: string | null;
  reviewerName: string;
  reviewerUserName: string;
  reviewerAvatarUrl: string | null;
  isAdminReview: boolean;
  createdAt: string;
}

export interface ItemRatingSummaryDto {
  averageRating: number;
  totalReviews: number;
  rating1Count: number;
  rating2Count: number;
  rating3Count: number;
  rating4Count: number;
  rating5Count: number;
}