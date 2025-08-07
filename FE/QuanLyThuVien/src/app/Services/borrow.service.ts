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
export class BorrowService {

  constructor(private baseApi: ApiBaseService) {}

  // Borrow endpoints
  getCurrentBorrows(): Observable<BorrowRecord[]> {
    return this.baseApi.get<BorrowRecord[]>(`borrow/current`);
  }

  getOverdueBorrows(): Observable<BorrowRecord[]> {
    return this.baseApi.get<BorrowRecord[]>(`borrow/overdue`);
  }

  getBorrowRecord(id: number): Observable<BorrowRecord> {
    return this.baseApi.get<BorrowRecord>(`borrow/${id}`);
  }

  createBorrow(request: BorrowRequestDto): Observable<BorrowRecord> {
    return this.baseApi.post<BorrowRecord>(`borrow`, request);
  }

  returnBook(id: number): Observable<BorrowRecord> {
    return this.baseApi.put<BorrowRecord>(`borrow/${id}/return`, {});
  }

  extendBorrow(id: number, request: any): Observable<BorrowRecord> {
    return this.baseApi.put<BorrowRecord>(`borrow/${id}/extend`, request);
  }

  payFine(id: number): Observable<any> {
    return this.baseApi.put(`borrow/${id}/pay-fine`, {});
  }
}