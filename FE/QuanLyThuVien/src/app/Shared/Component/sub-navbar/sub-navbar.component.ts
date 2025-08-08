import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../Services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  imports:[CommonModule],
  selector: 'app-sub-navbar',
  templateUrl: './sub-navbar.component.html',
  styleUrls: ['./sub-navbar.component.css']
})
export class SubNavbarComponent implements OnInit {
isLogin:boolean = false;
  constructor(private router: Router,
      private auth: AuthService,) { }

  ngOnInit() {
 this.isLogin = this.auth.isLoggedIn();
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
