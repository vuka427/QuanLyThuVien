import { Component, OnInit } from '@angular/core';
import { Author } from '../../Models/author';
import { AuthorService } from '../../Services/author.service';
import { SweetAlertService } from '../../Services/sweet-alert.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  imports: [FormsModule, CommonModule],
  selector: 'app-author-manage',
  templateUrl: './author-manage.component.html',
  styleUrls: ['./author-manage.component.css']
})
export class AuthorManageComponent implements OnInit {
  authors: Author[] = [];
  searchTerm: string = '';
  currentAuthor: any = this.initAuthor();
  isEditMode = false;

  constructor(
    private _authorService: AuthorService,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() {
    this.loadAuthors();
  }

  initAuthor() {
    return {
      fullName: '',
      penName: '',
      dateOfBirth: '',
      dateOfDeath: '',
      nationality: '',
      biography: '',
      isActive: true
    };
  }

  loadAuthors() {
    this._authorService.getAuthors().subscribe({
      next: (data) => {
        this.authors = data;
      },
      error: (error) => {
        this.sweetAlert.error('Không thể tải danh sách tác giả');
      }
    });
  }
  updateField(event: KeyboardEvent): void {
    console.log('The user pressed enter in the text field.');
  }

  public search() {
    if (this.searchTerm) {
      this._authorService.searchAuthors(this.searchTerm).subscribe({
        next: (data) => {
          this.authors = data;
        },
        error: (error) => {
          this.sweetAlert.error('Không thể tìm kiếm tác giả');
        }
      });
    } else {
      this.loadAuthors();
    }
  }

  openAuthorModal(author?: Author) {
    this.isEditMode = !!author;
    this.currentAuthor = author ? { ...author } : this.initAuthor();
    
    const modal = new (window as any).bootstrap.Modal(document.getElementById('authorModal'));
    modal.show();
  }

  editAuthor(author: Author) {
    this.openAuthorModal(author);
  }

  saveAuthor() {
    if (this.isEditMode) {
      this._authorService.updateAuthor(this.currentAuthor.authorId, this.currentAuthor).subscribe({
        next: () => {
          this.sweetAlert.success('Cập nhật tác giả thành công!');
          this.loadAuthors();
          this.closeModal();
        },
        error: (error) => {
          this.sweetAlert.error('Không thể cập nhật tác giả');
        }
      });
    } else {
      this._authorService.createAuthor(this.currentAuthor).subscribe({
        next: () => {
          this.sweetAlert.success('Thêm tác giả thành công!');
          this.loadAuthors();
          this.closeModal();
        },
        error: (error) => {
          this.sweetAlert.error('Không thể thêm tác giả');
        }
      });
    }
  }

  deleteAuthor(id: number) {
    this.sweetAlert.confirm('Bạn có chắc chắn muốn xóa tác giả này?', 'Xóa tác giả')
      .then((result) => {
        if (result.isConfirmed) {
          this._authorService.deleteAuthor(id).subscribe({
            next: () => {
              this.sweetAlert.success('Xóa tác giả thành công!');
              this.loadAuthors();
            },
            error: (error) => {
              this.sweetAlert.error('Không thể xóa tác giả');
            }
          });
        }
      });
  }

  closeModal() {
    const modal = (window as any).bootstrap.Modal.getInstance(document.getElementById('authorModal'));
    modal?.hide();
  }

}
