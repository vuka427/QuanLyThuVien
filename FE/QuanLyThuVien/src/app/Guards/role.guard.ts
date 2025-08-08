import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../Services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const requiredRole = route.data['role'] as string;
    const requiredPermissions = route.data['permissions'] as string[];
    
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return false;
    }

    // Kiểm tra role
    if (requiredRole && !this.authService.hasRole(requiredRole)) {
      this.router.navigate(['/unauthorized']);
      return false;
    }

    // Kiểm tra permissions
    // if (requiredPermissions && requiredPermissions.length > 0) {
    //   const hasAllPermissions = requiredPermissions.every(permission => 
    //     this.authService.hasPermission(permission)
    //   );
      
    //   if (!hasAllPermissions) {
    //     this.router.navigate(['/unauthorized']);
    //     return false;
    //   }
    // }

    return true;
  }
}