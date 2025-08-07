import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { StorageService } from './storage.service';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  statusCode: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiBaseService {


private readonly baseUrl = environment.apiUrl;
 private http = inject(HttpClient);
  private storageService = inject(StorageService);
  constructor() {}
  /**
   * Tạo headers cơ bản cho API calls
   */
  private getHeaders(): HttpHeaders {
    const token = this.storageService.getItem('token');
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    return headers;
  }

  /**
   * Xử lý lỗi từ API
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'Có lỗi xảy ra';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      switch (error.status) {
        case 401:
          errorMessage = 'Không có quyền truy cập';
          this.storageService.removeItem('token');
          break;
        case 403:
          errorMessage = 'Bị cấm truy cập';
          break;
        case 404:
          errorMessage = 'Không tìm thấy tài nguyên';
          break;
        case 500:
          errorMessage = 'Lỗi server';
          break;
        default:
          errorMessage = error.error?.message || 'Lỗi không xác định';
      }
    }

    return throwError(() => new Error(errorMessage));
  }

  /**
   * GET request
   */
  get<T>(endpoint: string, params?: any): Observable<T> {
    let httpParams = new HttpParams();
    
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }

    return this.http.get<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders(),
      params: httpParams
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * POST request
   */
  post<T>(endpoint: string, data?: any): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders()
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * PUT request
   */
  put<T>(endpoint: string, data?: any): Observable<T> {
    return this.http.put<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders()
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * PATCH request
   */
  patch<T>(endpoint: string, data?: any): Observable<T> {
    return this.http.patch<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, data, {
      headers: this.getHeaders()
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * DELETE request
   */
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, {
      headers: this.getHeaders()
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * Upload file
   */
  uploadFile<T>(endpoint: string, file: File, additionalData?: any): Observable<T> {
    const formData = new FormData();
    formData.append('file', file);
    
    if (additionalData) {
      Object.keys(additionalData).forEach(key => {
        formData.append(key, additionalData[key]);
      });
    }

    const token = this.storageService.getItem('token');
    let headers = new HttpHeaders();
    
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    return this.http.post<ApiResponse<T>>(`${this.baseUrl}/${endpoint}`, formData, {
      headers
    }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }
}
