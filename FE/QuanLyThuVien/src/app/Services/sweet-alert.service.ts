import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class SweetAlertService {

  success(message: string, title: string = 'Thành công!') {
    return Swal.fire({
      icon: 'success',
      title: title,
      text: message,
      confirmButtonColor: '#3085d6'
    });
  }

  error(message: string, title: string = 'Lỗi!') {
    return Swal.fire({
      icon: 'error',
      title: title,
      text: message,
      confirmButtonColor: '#d33'
    });
  }

  warning(message: string, title: string = 'Cảnh báo!') {
    return Swal.fire({
      icon: 'warning',
      title: title,
      text: message,
      confirmButtonColor: '#f0ad4e'
    });
  }

  info(message: string, title: string = 'Thông tin') {
    return Swal.fire({
      icon: 'info',
      title: title,
      text: message,
      confirmButtonColor: '#5bc0de'
    });
  }

  confirm(message: string, title: string = 'Xác nhận?') {
    return Swal.fire({
      title: title,
      text: message,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Đồng ý',
      cancelButtonText: 'Hủy bỏ'
    });
  }

  loading(title: string = 'Đang xử lý...') {
    return Swal.fire({
      title: title,
      allowOutsideClick: false,
      showConfirmButton: false,
      willOpen: () => {
        Swal.showLoading();
      }
    });
  }

  close() {
    Swal.close();
  }
}