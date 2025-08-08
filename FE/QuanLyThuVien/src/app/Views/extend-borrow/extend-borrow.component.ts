import { Component, OnInit } from '@angular/core';
import { BorrowRecord } from '../../Models/borrow-record';
import { BorrowService } from '../../Services/borrow.service';
import { MemberService } from '../../Services/member.service';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ExtendBorrowRequest } from '../../Models/extend-borrow-request.dto';

@Component({
  selector: 'app-extend-borrow',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './extend-borrow.component.html',
  styleUrls: ['./extend-borrow.component.css']
})
export class ExtendBorrowComponent implements OnInit {
  extendForm: FormGroup;
  currentBorrows: BorrowRecord[] = [];
  filteredBorrows: BorrowRecord[] = [];
  selectedBorrow: BorrowRecord | any | null = null;
  
  // Search and filter properties
  searchTerm: string = '';
  filterType: 'all' | 'due-soon' | 'extendable' = 'all';
  
  // UI state
  isLoading = false;
  isExtending = false;
  showSuccessMessage = false;
  showErrorMessage = false;
  errorMessage = '';
  successMessage = '';

  // Extension options
  extensionOptions = [
    { value: 7, label: '7 ngày', description: 'Gia hạn 1 tuần' },
    { value: 14, label: '14 ngày', description: 'Gia hạn 2 tuần' },
    { value: 21, label: '21 ngày', description: 'Gia hạn 3 tuần' },
    { value: 30, label: '30 ngày', description: 'Gia hạn 1 tháng' }
  ];

  constructor(
    private borrowService: BorrowService,
    private memberService: MemberService,
    private fb: FormBuilder
  ) {
    this.extendForm = this.fb.group({
      extendDays: [14, [Validators.required, Validators.min(1), Validators.max(90)]],
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
        // Chỉ lấy những sách chưa trả và có thể gia hạn
        this.currentBorrows = borrows.filter(b => 
          !b.isReturned && 
          (b.finePaid || b.fineAmount === 0) // Chỉ cho gia hạn nếu không có phạt hoặc đã trả phạt
        );
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
    const sevenDaysFromNow = new Date();
    sevenDaysFromNow.setDate(today.getDate() + 7);

    switch (this.filterType) {
      case 'due-soon':
        filtered = filtered.filter(borrow => {
          const dueDate = new Date(borrow.dueDate);
          return dueDate >= today && dueDate <= sevenDaysFromNow;
        });
        break;
      case 'extendable':
        filtered = filtered.filter(borrow => 
          this.canExtendBorrow(borrow)
        );
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
    this.extendForm.patchValue({
      extendDays: 14,
      notes: ''
    });
    this.clearMessages();
  }

  canExtendBorrow(borrow: BorrowRecord): boolean {
    // Logic để xác định có thể gia hạn hay không
    // Ví dụ: không có phạt chưa thanh toán, chưa gia hạn quá nhiều lần
    return (borrow.finePaid || borrow.fineAmount === 0) && 
           !this.isOverdue(borrow.dueDate);
  }

  extendBorrow(): void {
    if (!this.selectedBorrow || this.extendForm.invalid) {
      this.showError('Vui lòng kiểm tra thông tin và thử lại');
      return;
    }

    if (!this.canExtendBorrow(this.selectedBorrow)) {
      this.showError('Không thể gia hạn sách này. Vui lòng kiểm tra tình trạng phạt và hạn trả.');
      return;
    }

    this.isExtending = true;
    this.clearMessages();

    const extendRequest: ExtendBorrowRequest = {
      extendDays: this.extendForm.get('extendDays')?.value,
      notes: this.extendForm.get('notes')?.value || ''
    };

    this.borrowService.extendBorrow(this.selectedBorrow.borrowId, extendRequest).subscribe({
      next: (updatedRecord) => {
        const selectedOption = this.extensionOptions.find(opt => opt.value === extendRequest.extendDays);
        this.showSuccess(`Đã gia hạn sách "${this.selectedBorrow!.book.title}" thêm ${selectedOption?.label} thành công`);
        this.loadCurrentBorrows();
        this.selectedBorrow = null;
        this.extendForm.reset({ extendDays: 14, notes: '' });
        this.isExtending = false;
      },
      error: (error) => {
        this.showError('Không thể gia hạn sách. Vui lòng thử lại.');
        this.isExtending = false;
      }
    });
  }

  calculateNewDueDate(): Date | null {
    if (!this.selectedBorrow) return null;
    
    const extendDays = this.extendForm.get('extendDays')?.value;
    if (!extendDays) return null;

    const currentDueDate = new Date(this.selectedBorrow.dueDate);
    const newDueDate = new Date(currentDueDate);
    newDueDate.setDate(currentDueDate.getDate() + extendDays);
    
    return newDueDate;
  }

  getDaysUntilDue(dueDate: string): number {
    const today = new Date();
    const due = new Date(dueDate);
    const diffTime = due.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  }

  isOverdue(dueDate: string): boolean {
    return this.getDaysUntilDue(dueDate) < 0;
  }

  getStatusClass(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return 'table-danger'; // Overdue
    } else if (daysUntilDue <= 7) {
      return 'table-warning'; // Due soon
    }
    return ''; // Normal
  }

  getStatusBadgeClass(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return 'badge bg-danger';
    } else if (daysUntilDue <= 3) {
      return 'badge bg-danger';
    } else if (daysUntilDue <= 7) {
      return 'badge bg-warning text-dark';
    }
    return 'badge bg-success';
  }

  getStatusText(borrow: BorrowRecord): string {
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    
    if (daysUntilDue < 0) {
      return `Quá hạn ${Math.abs(daysUntilDue)} ngày`;
    } else if (daysUntilDue <= 7) {
      return `Còn ${daysUntilDue} ngày`;
    }
    return `Còn ${daysUntilDue} ngày`;
  }

  getExtensionBadgeClass(borrow: BorrowRecord): string {
    if (!this.canExtendBorrow(borrow)) {
      return 'badge bg-secondary';
    }
    
    const daysUntilDue = this.getDaysUntilDue(borrow.dueDate);
    if (daysUntilDue <= 7) {
      return 'badge bg-primary';
    }
    return 'badge bg-info';
  }

  getExtensionText(borrow: BorrowRecord): string {
    if (!this.canExtendBorrow(borrow)) {
      return 'Không thể gia hạn';
    }
    
    return 'Có thể gia hạn';
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
    this.extendForm.reset({ extendDays: 14, notes: '' });
    this.clearMessages();
  }

  trackByBorrowId(index: number, borrow: BorrowRecord|any): number {
    return borrow.borrowId;
  }


}