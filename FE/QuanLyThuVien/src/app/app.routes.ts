import { Routes } from '@angular/router';
import { MainLayoutComponent } from './Shared/Layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', loadComponent: () => import('./Views/home/home.component').then(m => m.HomeComponent) },
      { path: 'book', loadComponent: () => import('./Views/book-manage/book-manage.component').then(m => m.BookManageComponent) },
      { path: 'member', loadComponent: () => import('./Views/member-manage/member-manage.component').then(m => m.MemberManageComponent) },
      { path: 'author', loadComponent: () => import('./Views/author-manage/author-manage.component').then(m => m.AuthorManageComponent) },
      // các route chính khác
    ]
  },
  { path: 'login', loadComponent: () => import('./Views/account/login/login.component').then(m => m.LoginComponent)  }
];
