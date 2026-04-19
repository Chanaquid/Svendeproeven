export interface LoanSnapshotPhotoDto {
  id: number;
  loanId: number;
  photoUrl: string;
  displayOrder: number;
  snapshotTakenAt: string;
}

export interface LoanSnapshotPhotoListDto {
  id: number;
  photoUrl: string;
  displayOrder: number;
}