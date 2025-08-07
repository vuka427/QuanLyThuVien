import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap, map } from 'rxjs/operators';
import { AuthService } from '../Services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Add loading indicator
    this.showLoading();

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        this.hideLoading();

        if (error.status === 401 && !req.url.includes('auth/login')) {
          return this.handle401Error(req, next);
        }

        return throwError(() => error);
      }),
      // Hide loading on success
      map((event: HttpEvent<any>) => {
        if (event.type === 4) { // HttpResponse
          this.hideLoading();
        }
        return event;
      })
    );
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refreshToken().pipe(
        switchMap((response: any) => {
          this.isRefreshing = false;
          this.refreshTokenSubject.next(response.token);
          return next.handle(this.addTokenHeader(request, response.token));
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.authService.logout();
          return throwError(() => err);
        })
      );
    }

    return this.refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap((token) => next.handle(this.addTokenHeader(request, token)))
    );
  }

  private addTokenHeader(request: HttpRequest<any>, token: string): HttpRequest<any> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private showLoading(): void {
    // Implement loading indicator logic
    // Example: LoadingService.show()
  }

  private hideLoading(): void {
    // Implement loading indicator logic
    // Example: LoadingService.hide()
  }
}
