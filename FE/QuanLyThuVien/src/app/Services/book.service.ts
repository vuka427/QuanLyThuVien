import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Book } from '../Models/book';
import { BookCreateDto } from '../Models/book-create.dto';
import { Author } from '../Models/author';
import { Category } from '../Models/category';
import { Member } from '../Models/member';
import { BorrowRecord } from '../Models/borrow-record';
import { BorrowRequestDto } from '../Models/borrow-request.dto';
import { ApiBaseService } from './api-base.service';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  

  constructor(private baseApi: ApiBaseService) {}


  // Book endpoints
  getBooks(): Observable<Book[]> {
    return this.baseApi.get<Book[]>(`book` );
  }

  getBook(id: number): Observable<Book> {
    return this.baseApi.get<Book>(`book/${id}` );
  }

  createBook(book: BookCreateDto): Observable<Book> {
    return this.baseApi.post<Book>(`book`, book );
  }

  updateBook(id: number, book: any): Observable<any> {
    return this.baseApi.put(`book/${id}`, book );
  }

  deleteBook(id: number): Observable<any> {
    return this.baseApi.delete(`book/${id}` );
  }

  searchBooks(term?: string, categoryId?: number, authorId?: number): Observable<Book[]> {
    let params = new URLSearchParams();
    if (term) params.append('term', term);
    if (categoryId) params.append('categoryId', categoryId.toString());
    if (authorId) params.append('authorId', authorId.toString());
    
    return this.baseApi.get<Book[]>(`book/search?${params.toString()}`, 
      );
  }

  getAvailableBooks(): Observable<Book[]> {
    return this.baseApi.get<Book[]>(`book/available`, );
  }

  
}