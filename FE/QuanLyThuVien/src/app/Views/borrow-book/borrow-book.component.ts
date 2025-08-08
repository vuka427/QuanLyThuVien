import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { Member } from '../../Models/member';
import { Book } from '../../Models/book';
import { BorrowService } from '../../Services/borrow.service';
import { CommonModule } from '@angular/common';
import { BorrowRequestDto } from "../../Models/borrow-request.dto";



@Component({
  imports: [FormsModule,CommonModule, ReactiveFormsModule],
  selector: 'app-borrow-book',
  templateUrl: './borrow-book.component.html',
  styleUrls: ['./borrow-book.component.css']
})
export class BorrowBookComponent implements OnInit, OnDestroy {
  borrowForm!: FormGroup;
  private destroy$ = new Subject<void>();

  // Data
  availableBooks: Book[] = [];
  members: Member[] = [];
  filteredBooks: Book[] = [];
  filteredMembers: Member[] = [];

  // Search terms
  bookSearchTerm = '';
  memberSearchTerm = '';

  // UI State
  loading = false;
  submitting = false;
  selectedBook: Book | null = null;
  selectedMember: Member | null = null;

  // Validation messages
  validationMessages = {
    memberId: {
      required: 'Vui lòng chọn thành viên',
      min:"",
      max:""
    },
    bookId: {
      required: 'Vui lòng chọn sách',
      min:"",
      max:""
    },
    borrowDays: {
      required: 'Vui lòng nhập số ngày mượn',
      min: 'Số ngày mượn tối thiểu là 1',
      max: 'Số ngày mượn tối đa là 30'
    }
  };

  constructor(
    private fb: FormBuilder,
    private borrowService: BorrowService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    this.loadInitialData();
    this.setupFormSubscriptions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.borrowForm = this.fb.group({
      memberId: ['', Validators.required],
      bookId: ['', Validators.required],
      borrowDays: [14, [
        Validators.required,
        Validators.min(1),
        Validators.max(30)
      ]],
      notes: ['', Validators.maxLength(500)]
    });
  }

  private loadInitialData(): void {
    this.loading = true;

    forkJoin({
      books: this.borrowService.getAvailableBooks(),
      members: this.borrowService.getActiveMembers()
    }).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (data) => {
        this.availableBooks = data.books;
        this.members = data.members;
        this.filteredBooks = [...this.availableBooks];
        this.filteredMembers = [...this.members];
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading data:', error);
        this.loading = false;
        this.showError('Lỗi khi tải dữ liệu. Vui lòng thử lại.');
      }
    });
  }

  private setupFormSubscriptions(): void {
    // Watch for book selection changes
    this.borrowForm.get('bookId')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(bookId => {
        this.selectedBook = this.availableBooks.find(b => b.bookId == bookId) || null;
      });

    // Watch for member selection changes
    this.borrowForm.get('memberId')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(memberId => {
        this.selectedMember = this.members.find(m => m.memberId == memberId) || null;
        if (this.selectedMember) {
          this.checkMemberBorrowLimit();
        }
      });
  }

  // ==================== SEARCH METHODS ====================

  onBookSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.bookSearchTerm = target.value;
    this.filterBooks();
  }

  onMemberSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.memberSearchTerm = target.value;
    this.filterMembers();
  }

  private filterBooks(): void {
    const term = this.bookSearchTerm.toLowerCase().trim();
    
    if (!term) {
      this.filteredBooks = [...this.availableBooks];
      return;
    }

    this.filteredBooks = this.availableBooks.filter(book =>
      book.title.toLowerCase().includes(term) ||
      book.author.toLowerCase().includes(term) ||
      book.isbn.includes(term) ||
      (book.publisher && book.publisher.toLowerCase().includes(term))
    );
  }

  private filterMembers(): void {
    const term = this.memberSearchTerm.toLowerCase().trim();
    
    if (!term) {
      this.filteredMembers = [...this.members];
      return;
    }

    this.filteredMembers = this.members.filter(member =>
      member.fullName.toLowerCase().includes(term) ||
      member.memberCode.toLowerCase().includes(term) ||
      (member.email && member.email.toLowerCase().includes(term)) ||
      (member.phone && member.phone.includes(term))
    );
  }

  // ==================== FORM SUBMISSION ====================

  onSubmit(): void {
    if (this.borrowForm.valid && !this.submitting) {
      this.submitBorrowRequestDtoDto();
    } else {
      this.markFormGroupTouched();
    }
  }

  private submitBorrowRequestDtoDto(): void {
    this.submitting = true;
    
    const request: BorrowRequestDto = {
      memberId: this.borrowForm.value.memberId,
      bookId: this.borrowForm.value.bookId,
      borrowDays: this.borrowForm.value.borrowDays,
      notes: this.borrowForm.value.notes || undefined
    };

    this.borrowService.createBorrow(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.showSuccess('Mượn sách thành công!');
          this.resetForm();
          this.loadInitialData(); // Refresh data
        },
        error: (error) => {
          console.error('Error creating borrow:', error);
          this.showError(this.getErrorMessage(error));
        },
        complete: () => {
          this.submitting = false;
        }
      });
      this.submitting = false;
  }

  public resetForm(): void {
    this.borrowForm.reset();
    this.borrowForm.patchValue({ borrowDays: 14 });
    this.selectedBook = null;
    this.selectedMember = null;
    this.bookSearchTerm = '';
    this.memberSearchTerm = '';
    this.filteredBooks = [...this.availableBooks];
    this.filteredMembers = [...this.members];
  }

  private markFormGroupTouched(): void {
    Object.keys(this.borrowForm.controls).forEach(key => {
      const control = this.borrowForm.get(key);
      control?.markAsTouched();
    });
  }

  // ==================== VALIDATION HELPERS ====================

  isFieldInvalid(fieldName: string): boolean {
    const field = this.borrowForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.borrowForm.get(fieldName);
    if (field && field.errors && field.touched) {
      const errors = field.errors;
      const messages = this.validationMessages[fieldName as keyof typeof this.validationMessages];
      
      if (errors['required'] && messages?.required) {
        return messages.required;
      }
      if (errors['min'] && messages?.min) {
        return messages.min;
      }
      if (errors['max'] && messages?.max) {
        return messages.max;
      }
    }
    return '';
  }

  private checkMemberBorrowLimit(): void {
    if (this.selectedMember) {
      this.borrowService.canMemberBorrowMore(this.selectedMember.memberId||0)
        .pipe(takeUntil(this.destroy$))
        .subscribe(canBorrow => {
          if (!canBorrow) {
            this.showWarning('Thành viên đã đạt giới hạn mượn sách (tối đa 5 cuốn)');
          }
        });
    }
  }

  // ==================== COMPUTED PROPERTIES ====================

  get selectedBookInfo(): string {
    return this.selectedBook ? `${this.selectedBook.title} - ${this.selectedBook.author}` : '';
  }

  get selectedMemberInfo(): string {
    return this.selectedMember ? 
      `${this.selectedMember.memberCode} - ${this.selectedMember.fullName}` : '';
  }

  get calculatedDueDate(): Date | null {
    const borrowDays = this.borrowForm.get('borrowDays')?.value;
    return borrowDays ? this.borrowService.calculateDueDate(borrowDays) : null;
  }

  get isBookAvailable(): boolean {
    return this.selectedBook ? this.selectedBook.availableCopies > 0 : false;
  }

  // ==================== UTILITY METHODS ====================

  private showSuccess(message: string): void {
    // Implement your notification system here
    alert(message); // Temporary - replace with proper toast/notification
  }

  private showError(message: string): void {
    // Implement your notification system here
    alert(message); // Temporary - replace with proper toast/notification
  }

  private showWarning(message: string): void {
    // Implement your notification system here
    console.warn(message); // Temporary - replace with proper toast/notification
  }

  private getErrorMessage(error: any): string {
    if (error.error?.message) {
      return error.error.message;
    }
    if (error.status === 400) {
      return 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.';
    }
    if (error.status === 404) {
      return 'Không tìm thấy thông tin. Vui lòng thử lại.';
    }
    if (error.status === 500) {
      return 'Lỗi hệ thống. Vui lòng liên hệ quản trị viên.';
    }
    return 'Có lỗi xảy ra. Vui lòng thử lại sau.';
  }

  // ==================== PUBLIC UTILITY METHODS ====================

  refresh(): void {
    this.loadInitialData();
  }

  clearBookSearch(): void {
    this.bookSearchTerm = '';
    this.filterBooks();
  }

  clearMemberSearch(): void {
    this.memberSearchTerm = '';
    this.filterMembers();
  }
}