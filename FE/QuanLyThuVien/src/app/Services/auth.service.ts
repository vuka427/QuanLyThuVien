import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiBaseService, ApiResponse } from './api-base.service';
import { User } from '../Models/user';
import { StorageService } from './storage.service';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: User;
  expiration: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  private storageService = inject(StorageService);

  constructor(
    private baseApi: ApiBaseService,
    private router: Router
  ) {
    this.checkAuthStatus();
  }

  /**
   * Kiểm tra trạng thái authentication khi khởi tạo service
   */
  private checkAuthStatus(): void {
    const token = this.storageService.getItem('token');
    const userData = this.storageService.getItem('user');
    
    if (token && userData) {
      try {
        const user = JSON.parse(userData);
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
      } catch {
        this.logout();
      }
    }
  }

  /**
   * Đăng nhập
   */
  login(loginData: LoginRequest): Observable<LoginResponse> {
    return this.baseApi.post<LoginResponse>('auth/login', loginData).pipe(
      map(response => {
        if (response.token) {
          this.storageService.setItem('token', response.token);
          this.storageService.setItem('user', JSON.stringify(response.user));
          this.currentUserSubject.next(response.user);
          this.isAuthenticatedSubject.next(true);
        }
        return response;
      })
    );
  }

  /**
   * Đăng xuất
   */
  logout(): void {
    this.storageService.removeItem('token');
    this.storageService.removeItem('user');
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  /**
   * Kiểm tra xem user đã đăng nhập chưa
   */
  isLoggedIn(): boolean {
    return !!this.storageService.getItem('token');
  }

  /**
   * Lấy thông tin user hiện tại
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

 

  /**
   * Kiểm tra role của user
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
     return user?.role?.includes(role) || false;
  }

  /**
   * Refresh token
   */
  refreshToken(): Observable<any> {
    return this.baseApi.post('auth/refresh-token').pipe(
      map((response: any) => {
        if (response.token) {
          this.storageService.setItem('token', response.token);
        }
        return response;
      }),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }
}