import { ScoreChangeReason } from './enums';

export interface AdminAdjustScoreDto {
  userId: string;
  reason: ScoreChangeReason;
  loanId?: number | null;
  pointsChanged: number;
  note?: string | null;
}

export interface ScoreHistoryDto {
  id: number;
  pointsChanged: number;
  scoreAfterChange: number;
  reason: ScoreChangeReason;
  note: string | null;
  createdAt: string;
}

export interface UserScoreSummaryDto {
  currentScore: number;
  totalPointsEarned: number;
  totalPointsLost: number;
  totalScoreEvents: number;
  lastScoreChangeAt: string | null;
}