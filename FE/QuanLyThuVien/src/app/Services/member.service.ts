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
export class MemberService {
    constructor(private baseApi: ApiBaseService) {}



  // Member endpoints
  getMembers(): Observable<Member[]> {
    return this.baseApi.get<Member[]>(`member`);
  }

  getMember(id: number): Observable<Member> {
    return this.baseApi.get<Member>(`member/${id}`);
  }

  createMember(member: Member): Observable<Member> {
    return this.baseApi.post<Member>(`member`, member);
  }

  updateMember(id: number, member: Member): Observable<any> {
    return this.baseApi.put(`member/${id}`, member);
  }

  deleteMember(id: number): Observable<any> {
    return this.baseApi.delete(`member/${id}`);
  }

  searchMembers(term: string): Observable<Member[]> {
    return this.baseApi.get<Member[]>(`member/search?term=${term}`);
  }

  getMemberHistory(memberId: number): Observable<BorrowRecord[]> {
    return this.baseApi.get<BorrowRecord[]>(`member/${memberId}/history`);
  }
}