import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private isBrowser: boolean;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  /**
   * Set item to localStorage
   */
  setItem(key: string, value: string): void {
    if (this.isBrowser) {
      localStorage.setItem(key, value);
    }
  }

  /**
   * Get item from localStorage
   */
  getItem(key: string): string | null {
    if (this.isBrowser) {
      return localStorage.getItem(key);
    }
    return null;
  }

  /**
   * Remove item from localStorage
   */
  removeItem(key: string): void {
    if (this.isBrowser) {
      localStorage.removeItem(key);
    }
  }

  /**
   * Clear all localStorage
   */
  clear(): void {
    if (this.isBrowser) {
      localStorage.clear();
    }
  }

  /**
   * Check if running in browser
   */
  isBrowserEnvironment(): boolean {
    return this.isBrowser;
  }

  /**
   * Set object to localStorage
   */
  setObject(key: string, value: any): void {
    if (this.isBrowser) {
      localStorage.setItem(key, JSON.stringify(value));
    }
  }

  /**
   * Get object from localStorage
   */
  getObject<T>(key: string): T | null {
    if (this.isBrowser) {
      const item = localStorage.getItem(key);
      if (item) {
        try {
          return JSON.parse(item) as T;
        } catch {
          return null;
        }
      }
    }
    return null;
  }
}