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
export class AuthorService {
  constructor(private baseApi: ApiBaseService) {}

  // Author endpoints
  getAuthors(): Observable<Author[]> {
    return this.baseApi.get<Author[]>(`author`);
  }

  getAuthor(id: number): Observable<Author> {
    return this.baseApi.get<Author>(`author/${id}`);
  }

  createAuthor(author: Author): Observable<Author> {
    return this.baseApi.post<Author>(`author`, author);
  }

  updateAuthor(id: number, author: Author): Observable<any> {
    return this.baseApi.put(`author/${id}`, author);
  }

  deleteAuthor(id: number): Observable<any> {
    return this.baseApi.delete(`author/${id}`);
  }

  searchAuthors(term: string): Observable<Author[]> {
    return this.baseApi.get<Author[]>(`author/search?term=${term}` )
  }
}