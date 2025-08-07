import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Book } from '../../Models/book';
import { Category } from '../../Models/category';
import { Author } from '../../Models/author';
import { SweetAlertService } from '../../Services/sweet-alert.service';
import { BookService } from '../../Services/book.service';
import { CategoryService } from '../../Services/category.service';
import { AuthorService } from '../../Services/author.service';
import { CommonModule } from '@angular/common';

@Component({
  imports: [FormsModule, CommonModule],
  selector: 'app-book-manage',
  templateUrl: './book-manage.component.html',
  styleUrls: ['./book-manage.component.css']
})
export class BookManageComponent implements OnInit {

  books: Book[] = [];
  categories: Category[] = [];
  authors: Author[] = [];
  
  searchTerm: string = '';
  selectedCategoryId: number | '' = '';
  selectedAuthorId: number | '' = '';
  selectedAuthorIds: number[] = [];

  currentBook: any = this.initBook();
  isEditMode = false;

  constructor(
    private _bookService: BookService,
    private _categoryService: CategoryService,
    private _authorService : AuthorService,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() {
    this.loadBooks();
    this.loadCategories();
    this.loadAuthors();
  }

  initBook() {
    return {
      isbn: '',
      title: '',
      publisher: '',
      publishedDate: '',
      categoryId: '',
      totalCopies: 1
    };
  }

  loadBooks() {
    this._bookService.getBooks().subscribe({
      next: (data) => {
        this.books = data;
      },
      error: (error) => {
        this.sweetAlert.error('Không thể tải danh sách sách');
      }
    });
  }

  loadCategories() {
    this._categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
      }
    });
  }

  loadAuthors() {
    this._authorService.getAuthors().subscribe({
      next: (data) => {
        this.authors = data;
      }
    });
  }

  search() {
    const categoryId = this.selectedCategoryId === '' ? undefined : Number(this.selectedCategoryId);
    const authorId = this.selectedAuthorId === '' ? undefined : Number(this.selectedAuthorId);
    
    if (this.searchTerm || categoryId || authorId) {
      this._bookService.searchBooks(this.searchTerm, categoryId, authorId).subscribe({
        next: (data) => {
          this.books = data;
        },
        error: (error) => {
          this.sweetAlert.error('Không thể tìm kiếm sách');
        }
      });
    } else {
      this.loadBooks();
    }
  }

  openBookModal(book?: Book) {
    this.isEditMode = !!book;
    this.currentBook = book ? { ...book } : this.initBook();
    this.selectedAuthorIds = [];
    
    // Show modal using Bootstrap
    const modal = new (window as any).bootstrap.Modal(document.getElementById('bookModal'));
    modal.show();
  }

  editBook(book: Book) {
    this.openBookModal(book);
  }

  saveBook() {
    const bookData = {
      ...this.currentBook,
      authorIds: this.selectedAuthorIds
    };

    if (this.isEditMode) {
      this._bookService.updateBook(this.currentBook.bookId, bookData).subscribe({
        next: () => {
          this.sweetAlert.success('Cập nhật sách thành công!');
          this.loadBooks();
          this.closeModal();
        },
        error: (error) => {
          this.sweetAlert.error('Không thể cập nhật sách');
        }
      });
    } else {
      this._bookService.createBook(bookData).subscribe({
        next: () => {
          this.sweetAlert.success('Thêm sách thành công!');
          this.loadBooks();
          this.closeModal();
        },
        error: (error) => {
          this.sweetAlert.error('Không thể thêm sách');
        }
      });
    }
  }

  deleteBook(id: number) {
    this.sweetAlert.confirm('Bạn có chắc chắn muốn xóa sách này?', 'Xóa sách')
      .then((result) => {
        if (result.isConfirmed) {
          this._bookService.deleteBook(id).subscribe({
            next: () => {
              this.sweetAlert.success('Xóa sách thành công!');
              this.loadBooks();
            },
            error: (error) => {
              this.sweetAlert.error('Không thể xóa sách');
            }
          });
        }
      });
  }

  closeModal() {
    const modal = (window as any).bootstrap.Modal.getInstance(document.getElementById('bookModal'));
    modal?.hide();
  }

}
