export interface BookCreateDto {
  isbn: string;
  title: string;
  publisher?: string;
  publishedDate: string;
  categoryId: number;
  totalCopies: number;
  authorIds: number[];
}