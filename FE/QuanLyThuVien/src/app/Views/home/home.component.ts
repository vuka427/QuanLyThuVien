import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../Services/auth.service';
import { User } from '../../Models/user';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  imports: [FormsModule, CommonModule],
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  user?: User|any;
  isLogin:boolean = false; 

  constructor(private router: Router, private auth: AuthService) { }

  ngOnInit() {
    this.user = this.auth.getCurrentUser();
    this.isLogin = this.auth.isLoggedIn();
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
    this.router.navigate(["extend-book"])
  }
  navigateSearchBook() {
    this.router.navigate(["search-book"])
  }

}
