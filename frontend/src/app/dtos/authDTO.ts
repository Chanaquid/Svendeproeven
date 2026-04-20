import { UserListForUsersDto } from "./userDTO";

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  user: UserListForUsersDto;
}

export interface VerifyPhoneDto {
  code: string;
}

export interface VerifyEmailDto {
  userId: string;
  token: string;
}

export interface RefreshTokenRequestDto {
  refreshToken: string;
}

export interface RefreshTokenResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
}

export interface RevokeTokenRequestDto {
  refreshToken: string;
}

export interface ResendConfirmationDto {
  email: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordDto {
  email: string;
}

export interface ResetPasswordDto {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface AuthResponseDto {
  token: string;
  refreshToken: string;
  userId: string;
  fullName: string;
  username: string;
  avatarUrl: string | null;
  isVerified: boolean;
  email: string;
  role: string;
  score: number;
  unpaidFinesTotal: number;
  expiresAt: string;
}