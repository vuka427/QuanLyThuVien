import {Category} from './category'

export interface Book {
  bookId?: number;
  isbn: string;
  title: string;
  author: string;
  publisher?: string;
  publishedDate: string;
  categoryId: number;
  totalCopies: number;
  availableCopies: number;
  createdDate?: string;
  category?: Category;
  authorsDisplay?: string;
}