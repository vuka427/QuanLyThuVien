import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../Services/auth.service';
import { CommonModule } from '@angular/common';
import {SubNavbarComponent} from '../../../Shared/Component/sub-navbar/sub-navbar.component'

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, CommonModule, SubNavbarComponent],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css']
})
export class MainLayoutComponent implements OnInit {
  isLogin:boolean = false; 
  constructor(private router: Router,  private auth: AuthService) { }

  ngOnInit() {
    this.isLogin = this.auth.isLoggedIn();
  }
  navigateHome() {
    this.router.navigate(["home"])
  }
  navigateBookManage() {
    this.router.navigate(["book"])
  }
  navigateMemberManage(){
    this.router.navigate(["member"])
  }
   navigateAuthorManage(){
    this.router.navigate(["author"])
  }
  logout(){
    this.auth.logout();
  }

}
