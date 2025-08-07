export interface BorrowRequestDto {
  memberId: number;
  bookId: number;
  borrowDays?: number;
  notes?: string;
}