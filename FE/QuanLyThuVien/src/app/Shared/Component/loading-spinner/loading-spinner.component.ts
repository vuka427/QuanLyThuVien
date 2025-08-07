import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';

@Component({
  imports: [CommonModule],
  selector: 'app-loading-spinner',
  templateUrl: './loading-spinner.component.html',
  styleUrls: ['./loading-spinner.component.css']
})
export class LoadingSpinnerComponent implements OnInit {
  @Input() isLoading: boolean = false;
  @Input() type: 'default' | 'dots' | 'pulse' = 'default';
  @Input() message: string = '';
  constructor() { }

  ngOnInit() {
  }

}
