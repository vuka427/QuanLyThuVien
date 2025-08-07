import { Component, OnInit } from '@angular/core';
import { Member } from '../../Models/member';
import { BorrowRecord } from '../../Models/borrow-record';
import { MemberService } from '../../Services/member.service';
import { SweetAlertService } from '../../Services/sweet-alert.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  imports: [FormsModule, CommonModule],
  selector: 'app-member-manage',
  templateUrl: './member-manage.component.html',
  styleUrls: ['./member-manage.component.css']
})
export class MemberManageComponent implements OnInit {
  members: Member[] = [];
  memberHistory: BorrowRecord[] = [];
  
  searchTerm: string = '';
  currentMember: any = this.initMember();
  isEditMode = false;

  constructor(
    private _memberService: MemberService,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit() {
    this.loadMembers();
  }

  initMember() {
    return {
      memberCode: '',
      fullName: '',
      email: '',
      phone: '',
      address: '',
      dateOfBirth: '',
      membershipDate: new Date().toISOString().split('T')[0],
      isActive: true
    };
  }

  loadMembers() {
    this._memberService.getMembers().subscribe({
      next: (data:Member[]) => {
        console.log("mợ nó",data)
        this.members = data;
      },
      error: (error) => {
        this.sweetAlert.error('Không thể tải danh sách thành viên');
      }
    });
  }

  search() {
    if (this.searchTerm) {
      this._memberService.searchMembers(this.searchTerm).subscribe({
        next: (data) => {
          this.members = data;
        },
        error: (error) => {
          this.sweetAlert.error('Không thể tìm kiếm thành viên');
        }
      });
    } else {
      this.loadMembers();
    }
  }

  openMemberModal(member?: Member) {
    this.isEditMode = !!member;
    this.currentMember = member ? { ...member } : this.initMember();
    
    const modal = new (window as any).bootstrap.Modal(document.getElementById('memberModal'));
    modal.show();
  }

  editMember(member: Member) {
    this.openMemberModal(member);
  }

  saveMember() {
    if (this.isEditMode) {
      this._memberService.updateMember(this.currentMember.memberId, this.currentMember).subscribe({
        next: () => {
          this.sweetAlert.success('Cập nhật thành viên thành công!');
          this.loadMembers();
          this.closeModal('memberModal');
        },
        error: (error) => {
          this.sweetAlert.error('Không thể cập nhật thành viên');
        }
      });
    } else {
      this._memberService.createMember(this.currentMember).subscribe({
        next: () => {
          this.sweetAlert.success('Thêm thành viên thành công!');
          this.loadMembers();
          this.closeModal('memberModal');
        },
        error: (error) => {
          this.sweetAlert.error('Không thể thêm thành viên');
        }
      });
    }
  }

  closeModal( name: string) {
    const modal = (window as any).bootstrap.Modal.getInstance(document.getElementById(name));
    modal?.hide();
  }
  
  viewMemberHistory(memberId: number) {
    this._memberService.getMemberHistory(memberId).subscribe({
      next: (data) => {
        this.memberHistory = data;
        const modal = new (window as any).bootstrap.Modal(document.getElementById('historyModal'));
        modal.show();
      },
      error: () => {
        this.sweetAlert.error('Không thể tải lịch sử mượn sách');
      }
    });
  }
  
  toggleMemberStatus(memberId: number, isActive: boolean) {
    const member = this.members.find(m => m.memberId === memberId);
    if (!member) return;
    const updatedMember = { ...member, isActive: !isActive };
    this._memberService.updateMember(memberId, updatedMember).subscribe({
      next: () => {
        this.sweetAlert.success('Cập nhật trạng thái thành viên thành công!');
        this.loadMembers();
      },
      error: () => {
        this.sweetAlert.error('Không thể cập nhật trạng thái thành viên');
      }
    });
  }
  
  deleteMember(memberId: number) {
    this.sweetAlert.warning('Bạn có chắc muốn xóa thành viên này?', 'Xác nhận xóa').then((result: any) => {
      if (result.isConfirmed) {
        this._memberService.deleteMember(memberId).subscribe({
          next: () => {
            this.sweetAlert.success('Xóa thành viên thành công!');
            this.loadMembers();
          },
          error: () => {
            this.sweetAlert.error('Không thể xóa thành viên');
          }
        });
      }
    });
  }
  
}
