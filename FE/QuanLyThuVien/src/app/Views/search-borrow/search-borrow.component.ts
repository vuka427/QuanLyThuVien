import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BorrowRecord } from '../../Models/borrow-record';
import { Category } from '../../Models/category';
import { Member } from '../../Models/member';
import { Author } from '../../Models/author';
import { BorrowService } from '../../Services/borrow.service';
import { CategoryService } from '../../Services/category.service';
import { AuthorService } from '../../Services/author.service';
import { MemberService } from '../../Services/member.service';
import { debounceTime, distinctUntilChanged, Observable, of, switchMap } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  selector: 'app-search-borrow',
  templateUrl: './search-borrow.component.html',
  styleUrls: ['./search-borrow.component.css']
})
export class SearchBorrowComponent implements OnInit {
  searchForm: FormGroup;
  borrowRecords: BorrowRecord[] = [];
  filteredBorrows: BorrowRecord[] = [];
  categories: Category[] = [];
  authors: Author[] = [];
  members: Member[] = [];
  
  isLoading = false;
  showFilters = false;
  currentPage = 1;
  itemsPerPage = 10;
  totalItems = 0;

  // Status options
  statusOptions = [
    { value: '', label: 'Tất cả trạng thái' },
    { value: 'current', label: 'Đang mượn' },
    { value: 'overdue', label: 'Quá hạn' },
    { value: 'returned', label: 'Đã trả' }
  ];

  constructor(
    private fb: FormBuilder,
    private borrowService: BorrowService,
    private categoryService: CategoryService,
    private authorService: AuthorService,
    private memberService: MemberService
  ) {
    this.searchForm = this.fb.group({
      term: [''],
      categoryId: [''],
      authorId: [''],
      memberId: [''],
      status: ['']
    });
  }

  ngOnInit(): void {
    this.loadInitialData();
    this.setupSearch();
    this.loadAllBorrows();
  }

  loadInitialData(): void {
    // Load categories
    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories.filter(c => c.isActive);
      },
      error: (error) => console.error('Error loading categories:', error)
    });

    // Load authors
    this.authorService.getAuthors().subscribe({
      next: (authors) => {
        this.authors = authors;
      },
      error: (error) => console.error('Error loading authors:', error)
    });

    // Load active members
    this.memberService.getMembers().subscribe({
      next: (members) => {
        this.members = members.filter(m => m.isActive);
      },
      error: (error) => console.error('Error loading members:', error)
    });
  }

  setupSearch(): void {
    // Setup debounced search
    this.searchForm.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(() => {
        return this.performSearch();
      })
    ).subscribe({
      next: (results) => {
        this.filteredBorrows = results;
        this.totalItems = results.length;
        this.currentPage = 1;
      },
      error: (error) => {
        console.error('Search error:', error);
        this.filteredBorrows = [];
      }
    });
  }

  loadAllBorrows(): void {
    this.isLoading = true;
    
    // Load current borrows
    this.borrowService.getCurrentBorrows().subscribe({
      next: (current) => {
        this.borrowRecords = [...current];
        
        // Load overdue borrows and merge
        this.borrowService.getOverdueBorrows().subscribe({
          next: (overdue) => {
            // Merge and remove duplicates
            const allBorrows = [...current, ...overdue];
            this.borrowRecords = allBorrows.filter((borrow, index, self) => 
              index === self.findIndex(b => b.borrowId === borrow.borrowId)
            );
            
            this.filteredBorrows = [...this.borrowRecords];
            this.totalItems = this.borrowRecords.length;
            this.isLoading = false;
          },
          error: (error) => {
            console.error('Error loading overdue borrows:', error);
            this.filteredBorrows = [...this.borrowRecords];
            this.totalItems = this.borrowRecords.length;
            this.isLoading = false;
          }
        });
      },
      error: (error) => {
        console.error('Error loading current borrows:', error);
        this.isLoading = false;
      }
    });
  }

  performSearch(): Observable<BorrowRecord[]> {
    const params = this.searchForm.value;
    
    if (this.isEmptySearch(params)) {
      return of(this.borrowRecords);
    }

    return of(this.borrowRecords.filter(borrow => this.matchesSearchCriteria(borrow, params)));
  }

  private isEmptySearch(params: any): boolean {
    return !params.term && !params.categoryId && !params.authorId && !params.memberId && !params.status;
  }

  private matchesSearchCriteria(borrow: BorrowRecord, params: any): boolean {
    // Search term - search in book title, member name, member code
    if (params.term) {
      const term = params.term.toLowerCase();
      const matchesBook = borrow.book?.title?.toLowerCase().includes(term) ||
                         borrow.book?.author?.toLowerCase().includes(term) ||
                         borrow.book?.isbn?.toLowerCase().includes(term);
      const matchesMember = borrow.member?.fullName?.toLowerCase().includes(term) ||
                           borrow.member?.memberCode?.toLowerCase().includes(term);
      
      if (!matchesBook && !matchesMember) {
        return false;
      }
    }

    // Category filter
    if (params.categoryId && borrow.book?.categoryId !== +params.categoryId) {
      return false;
    }

    // Member filter
    if (params.memberId && borrow.memberId !== +params.memberId) {
      return false;
    }

    // Status filter
    if (params.status) {
      const now = new Date();
      const dueDate = new Date(borrow.dueDate);
      
      switch (params.status) {
        case 'current':
          if (borrow.isReturned || dueDate < now) {
            return false;
          }
          break;
        case 'overdue':
          if (borrow.isReturned || dueDate >= now) {
            return false;
          }
          break;
        case 'returned':
          if (!borrow.isReturned) {
            return false;
          }
          break;
      }
    }

    return true;
  }

  clearSearch(): void {
    this.searchForm.reset({
      term: '',
      categoryId: '',
      authorId: '',
      memberId: '',
      status: ''
    });
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  getBorrowStatus(borrow: BorrowRecord): { status: string; class: string; text: string } {
    if (borrow.isReturned) {
      return { status: 'returned', class: 'success', text: 'Đã trả' };
    }
    
    const now = new Date();
    const dueDate = new Date(borrow.dueDate);
    
    if (dueDate < now) {
      return { status: 'overdue', class: 'danger', text: 'Quá hạn' };
    }
    
    return { status: 'current', class: 'primary', text: 'Đang mượn' };
  }

  getDaysOverdue(dueDate: string): number {
    const now = new Date();
    const due = new Date(dueDate);
    const diffTime = now.getTime() - due.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('vi-VN');
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  }

  // Pagination
  get paginatedBorrows(): BorrowRecord[] {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredBorrows.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.itemsPerPage);
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  changeItemsPerPage(items: number|any): void {
    this.itemsPerPage = items;
    this.currentPage = 1;
  }

  onReturnBook(borrowId: number|any): void {
    if (confirm('Bạn có chắc chắn muốn trả sách này?')) {
      this.borrowService.returnBook(borrowId).subscribe({
        next: (updatedBorrow) => {
          const index = this.borrowRecords.findIndex(b => b.borrowId === borrowId);
          if (index !== -1) {
            this.borrowRecords[index] = updatedBorrow;
            this.performSearch().subscribe(results => {
              this.filteredBorrows = results;
            });
          }
          alert('Trả sách thành công!');
        },
        error: (error) => {
          console.error('Error returning book:', error);
          alert('Có lỗi xảy ra khi trả sách!');
        }
      });
    }
  }

  onPayFine(borrowId: number|any): void {
    if (confirm('Bạn có chắc chắn muốn thanh toán phí phạt?')) {
      this.borrowService.payFine(borrowId).subscribe({
        next: () => {
          const index = this.borrowRecords.findIndex(b => b.borrowId === borrowId);
          if (index !== -1) {
            this.borrowRecords[index].finePaid = true;
            this.performSearch().subscribe(results => {
              this.filteredBorrows = results;
            });
          }
          alert('Thanh toán phí phạt thành công!');
        },
        error: (error) => {
          console.error('Error paying fine:', error);
          alert('Có lỗi xảy ra khi thanh toán phí phạt!');
        }
      });
    }
  }

  exportToCSV(): void {
    // Simple CSV export functionality
    const headers = ['ID', 'Thành viên', 'Sách', 'Ngày mượn', 'Ngày hết hạn', 'Trạng thái', 'Phí phạt'];
    const csvContent = [
      headers.join(','),
      ...this.filteredBorrows.map(borrow => [
        borrow.borrowId,
        `"${borrow.member?.fullName}"`,
        `"${borrow.book?.title}"`,
        this.formatDate(borrow.borrowDate),
        this.formatDate(borrow.dueDate),
        this.getBorrowStatus(borrow).text,
        borrow.fineAmount
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `borrow-records-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
  getMinValue(a: number, b: number): number {
    return Math.min(a, b);
  }
  get currentBorrowsCount(): number {
    const now = new Date();
    return this.filteredBorrows.filter(b => !b.isReturned && new Date(b.dueDate) >= now).length;
  }

  get overdueBorrowsCount(): number {
    const now = new Date();
    return this.filteredBorrows.filter(b => !b.isReturned && new Date(b.dueDate) < now).length;
  }

  get returnedBorrowsCount(): number {
    return this.filteredBorrows.filter(b => b.isReturned).length;
  }
}
