import { BorrowingStatus } from "./enums";

export interface RegisterUserRequestDto {
  fullName: string;
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string | null;
  address: string;
  latitude?: number | null;
  longitude?: number | null;
  dateOfBirth: string;
  gender?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
}

export interface RegisterUserResponseDto {
  userId: string;
  email: string;
  avatarUrl: string | null;
  username: string;
  message: string;
}

export interface UpdateProfileDto {
  fullName?: string | null;
  userName?: string | null;
  email?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  gender?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  dateOfBirth?: string | null;
}

export interface UpdateAvatarDto {
  avatarUrl: string;
}

export interface DeleteAccountDto {
  password: string;
  reason?: string | null;
}

export interface UserProfileDto {
  id: string;
  fullName: string;
  username: string;
  email: string;
  phoneNumber: string | null;
  gender: string | null;
  role: string;
  bio: string | null;
  address: string;
  latitude: number | null;
  longitude: number | null;
  dateOfBirth: string;
  age: number;
  avatarUrl: string | null;
  score: number;
  unpaidFinesTotal: number;
  isVerified: boolean;
  isBanned: boolean;
  totalCompletedLoans: number;
  borrowingStatus: BorrowingStatus;
  membershipDate: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface UserListForUsersDto {
  id: string;
  username: string;
  fullName: string;
  avatarUrl: string | null;
  score: number;
  isVerified: boolean;
  totalItems: number;
  averageRating: number | null;
  membershipDate: string;
}

export interface UserPublicProfileDto {
  id: string;
  fullName: string;
  username: string;
  avatarUrl: string | null;
  bio: string | null;
  gender: string | null;
  age: number;
  isVerified: boolean;
  score: number;
  membershipDate: string;
  generalAddress: string | null;
  totalItems: number;
  totalReviewsReceived: number;
  totalCompletedLoans: number;

}