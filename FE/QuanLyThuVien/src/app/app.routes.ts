import { Routes } from '@angular/router';
import { MainLayoutComponent } from './Shared/Layout/main-layout/main-layout.component';
import { AuthGuard } from './Guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { 
        path: 'home', 
        loadComponent: () => import('./Views/home/home.component').then(m => m.HomeComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'book', 
        loadComponent: () => import('./Views/book-manage/book-manage.component').then(m => m.BookManageComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'member', 
        loadComponent: () => import('./Views/member-manage/member-manage.component').then(m => m.MemberManageComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'author', 
        loadComponent: () => import('./Views/author-manage/author-manage.component').then(m => m.AuthorManageComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: '', 
        loadComponent: () => import('./Views/author-manage/author-manage.component').then(m => m.AuthorManageComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'borrow-book', 
        loadComponent: () => import('./Views/borrow-book/borrow-book.component').then(m => m.BorrowBookComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'return-book', 
        loadComponent: () => import('./Views/return-book/return-book.component').then(m => m.ReturnBookComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'extend-borrow', 
        loadComponent: () => import('./Views/extend-borrow/extend-borrow.component').then(m => m.ExtendBorrowComponent), 
        canActivate: [AuthGuard]
      },
      { 
        path: 'search-book', 
        loadComponent: () => import('./Views/search-borrow/search-borrow.component').then(m => m.SearchBorrowComponent), 
        canActivate: [AuthGuard]
      },
      // các route chính khác
    ]
  },
  { 
    path: 'login', 
    loadComponent: () => import('./Views/account/login/login.component').then(m => m.LoginComponent)  
  }
];
