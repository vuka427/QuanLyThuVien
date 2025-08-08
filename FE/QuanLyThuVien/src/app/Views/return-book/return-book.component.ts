import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { BorrowRecord } from '../../Models/borrow-record';
import { BorrowService } from '../../Services/borrow.service';

@Component({
  selector: 'app-return-book',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './return-book.component.html',
  styleUrls: ['./return-book.component.css']
})
export class ReturnBookComponent implements OnInit {
  returnForm: FormGroup;
  currentBorrows: BorrowRecord[] = [];
  filteredBorrows: any | BorrowRecord[] = [];
  selectedBorrow: BorrowRecord | any;
  
  // Search and filter properties
  searchTerm: string = '';
  filterType: 'all' | 'overdue' | 'due-soon' = 'all';
  
  // UI state
  isLoading = false;
  isReturning = false;
  showSuccessMessage = false;
  showErrorMessage = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private borrowService: BorrowService,
    private fb: FormBuilder
  ) {
    this.returnForm = this.fb.group({
      notes: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    this.loadCurrentBorrows();
  }

  loadCurrentBorrows(): void {
    this.isLoading = true;
    this.borrowService.getCurrentBorrows().subscribe({
      next: (borrows) => {
        this.currentBorrows = borrows.filter(b => !b.isReturned);
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        this.showError('Không thể tải danh sách sách đang mượn');
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = this.currentBorrows;

    // Apply search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(borrow => 
        borrow.book?.title.toLowerCase().includes(term) ||
        borrow.book?.author.toLowerCase().includes(term) ||
        borrow.member?.fullName.toLowerCase().includes(term) ||
        borrow.member?.memberCode.toLowerCase().includes(term)
      );
    }

    // Apply status filter
    const today = new Date();
    const threeDaysFromNow = new Date();
    threeDaysFromNow.setDate(today.getDate() + 3);

    switch (this.filterType) {
      case 'overdue':
        filtered = filtered.filter(borrow => new Date(borrow.dueDate) < today);
        break;
      case 'due-soon':
        filtered = filtered.filter(borrow => {
          const dueDate = new Date(borrow.dueDate);
          return dueDate >= today && dueDate <= threeDaysFromNow;
        });
        break;
      default:
        // 'all' - no additional filtering
        break;
    }

    this.filteredBorrows = filtered.sort((a, b) => 
      new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    );
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  selectBorrow(borrow: BorrowRecord): void {
    this.selectedBorrow = borrow;
    this.returnForm.patchValue({
      notes: ''
    });
    this.clearMessages();
  }

  returnBook(): void {
    if (!this.selectedBorrow) {
      this.showError('Vui lòng chọn sách cần trả');
      return;
    }

    this.isReturning = true;
    this.clearMessages();

    const notes = this.returnForm.get('notes')?.value;
    
    this.borrowService.returnBook(this.selectedBorrow.borrowId).subscribe({
      next: (updatedRecord) => {
        this.showSuccess(`Đã trả sách "${this.selectedBorrow?.book?.title}" thành công`);
        this.loadCurrentBorrows();
        this.selectedBorrow = null;
        this.returnForm.reset();
        this.isReturning = false;
      },
      error: (error) => {
        this.showError('Không thể trả sách. Vui lòng thử lại.');
        this.isReturning = false;
      }
    });
  }

  payFine(borrowId: number): void {
    this.borrowService.payFine(borrowId).subscribe({
      next: () => {
        this.showSuccess('Đã thanh toán phạt thành công');
        this.loadCurrentBorrows();
      },
      error: () => {
        this.showError('Không thể thanh toán phạt');
      }
    });
  }

  calculateOverdueDays(dueDate: string): number {
    const today = new Date();
    const due = new Date(dueDate);
    const diffTime = today.getTime() - due.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return Math.max(0, diffDays);
  }

  getDaysUntilDue(dueDate: string): number {
    const today = new Date();
    const due = new Date(dueDate);
    const diffTime = due.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  }

  getStatusClass(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return 'table-danger'; // Overdue
    } else if (daysUntilDue <= 3) {
      return 'table-warning'; // Due soon
    }
    return ''; // Normal
  }

  getStatusBadgeClass(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return 'badge bg-danger';
    } else if (daysUntilDue <= 3) {
      return 'badge bg-warning text-dark';
    }
    return 'badge bg-success';
  }

  getStatusText(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return `Quá hạn ${Math.abs(daysUntilDue)} ngày`;
    } else if (daysUntilDue <= 3) {
      return `Còn ${daysUntilDue} ngày`;
    }
    return `Còn ${daysUntilDue} ngày`;
  }

  private showSuccess(message: string): void {
    this.successMessage = message;
    this.showSuccessMessage = true;
    this.showErrorMessage = false;
    setTimeout(() => {
      this.showSuccessMessage = false;
    }, 5000);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.showErrorMessage = true;
    this.showSuccessMessage = false;
  }

  private clearMessages(): void {
    this.showSuccessMessage = false;
    this.showErrorMessage = false;
  }

  clearSelection(): void {
    this.selectedBorrow = null;
    this.returnForm.reset();
    this.clearMessages();
  }

  // Hàm trackBy
  trackByBorrowId(index: number, item: BorrowRecord): number {
    return item.memberId;
  }
}
