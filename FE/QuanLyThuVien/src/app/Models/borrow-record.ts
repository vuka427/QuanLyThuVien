import { Book } from "./book";
import { Member } from "./member";

export interface BorrowRecord {
  borrowId?: number;
  memberId: number;
  bookId: number;
  borrowDate: string;
  dueDate: string;
  returnDate?: string;
  isReturned: boolean;
  fineAmount: number;
  finePaid: boolean;
  notes?: string;
  member?: Member;
  book?: Book ;
}