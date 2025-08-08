export interface BorrowSearchParams {
  term?: string;
  categoryId?: number;
  authorId?: number;
  memberId?: number;
  status?: 'current' | 'overdue' | 'returned';
}