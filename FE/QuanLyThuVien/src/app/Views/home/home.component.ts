import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../Services/auth.service';
import { User } from '../../Models/user';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { BorrowService } from '../../Services/borrow.service';
import { BorrowRecord } from '../../Models/borrow-record';

@Component({
  imports: [FormsModule, CommonModule],
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  user?: User|any;
  isLogin:boolean = false;
  recentTransactions: BorrowRecord[] = [];
  currentDate: Date = new Date();

  constructor(
    private router: Router, 
    private auth: AuthService,
    private borrowService: BorrowService
  ) { }

  ngOnInit() {
    this.user = this.auth.getCurrentUser();
    this.isLogin = this.auth.isLoggedIn();
    if (this.isLogin) {
      this.loadRecentTransactions();
    }
    // Update current date every minute
    setInterval(() => {
      this.currentDate = new Date();
    }, 60000);
  }

  isOverdue(transaction: BorrowRecord): boolean {
    if (transaction.isReturned) return false;
    const dueDate = new Date(transaction.dueDate);
    return dueDate < this.currentDate;
  }

  loadRecentTransactions() {
    this.borrowService.getCurrentBorrows().subscribe({
      next: (data) => {
        this.recentTransactions = data;
      },
      error: (error) => {
        console.error('Error loading recent transactions:', error);
      }
    });
  }

  navigateLogin() {
    this.router.navigate(["login"])
  }
  navigateBorrowBook() {
    this.router.navigate(["borrow-book"])
  }
  navigateReturnBook() {
    this.router.navigate(["return-book"])
  }
  
  navigateExtendBook() {
    this.router.navigate(["extend-borrow"])
  }
  navigateSearchBook() {
    this.router.navigate(["search-book"])
  }

}
