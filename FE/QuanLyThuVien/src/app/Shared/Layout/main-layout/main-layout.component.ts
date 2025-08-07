import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css']
})
export class MainLayoutComponent implements OnInit {

  constructor(private router: Router) { }

  ngOnInit() {

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

}
