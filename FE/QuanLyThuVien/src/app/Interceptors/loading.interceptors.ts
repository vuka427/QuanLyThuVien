import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, finalize } from 'rxjs/operators';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  private activeRequests = 0;

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip loading for certain endpoints
    if (this.shouldSkipLoading(req.url)) {
      return next.handle(req);
    }

    this.activeRequests++;
    this.updateLoadingStatus();

    return next.handle(req).pipe(
      finalize(() => {
        this.activeRequests--;
        this.updateLoadingStatus();
      })
    );
  }

  private shouldSkipLoading(url: string): boolean {
    const skipUrls = ['/auth/refresh-token', '/heartbeat'];
    return skipUrls.some(skipUrl => url.includes(skipUrl));
  }

  private updateLoadingStatus(): void {
    // Emit loading status to a service or component
    // Example: LoadingService.setLoading(this.activeRequests > 0)
  }
}
