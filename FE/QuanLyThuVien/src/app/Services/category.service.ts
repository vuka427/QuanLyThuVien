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
export class CategoryService {
    constructor(private baseApi: ApiBaseService) {}

  

  // Category endpoints
  getCategories(): Observable<Category[]> {
    return this.baseApi.get<Category[]>(`category`);
  }

  getCategory(id: number): Observable<Category> {
    return this.baseApi.get<Category>(`category/${id}`);
  }

  createCategory(category: Category): Observable<Category> {
    return this.baseApi.post<Category>(`category`, category);
  }

  updateCategory(id: number, category: Category): Observable<any> {
    return this.baseApi.put(`category/${id}`, category);
  }

  deleteCategory(id: number): Observable<any> {
    return this.baseApi.delete(`category/${id}`);
  }

  
}