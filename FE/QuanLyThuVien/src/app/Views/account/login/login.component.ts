import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, LoginRequest } from '../../../Services/auth.service';
import { StorageService } from '../../../Services/storage.service';
import { SweetAlertService } from '../../../Services/sweet-alert.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

 loginForm!: FormGroup;
  isLoading = false;
  showPassword = false;
  errorMessage = '';
  rememberMe = false;
    private storageService = inject(StorageService);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private sweetAlert: SweetAlertService
  ) {}

  ngOnInit(): void {
    this.createForm();
    this.loadRememberedCredentials();
  }

  private createForm(): void {
    this.loginForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(3)]],
      rememberMe: [false]
    });
  }

  private loadRememberedCredentials(): void {
    const rememberedUsername = this.storageService.getItem('rememberedUsername');
    if (rememberedUsername) {
      this.loginForm.patchValue({
        username: rememberedUsername,
        rememberMe: true
      });
    }
  }

  onSubmit(): void {
    if (this.loginForm.valid && !this.isLoading) {
      this.isLoading = true;
      this.errorMessage = '';

      const loginData: LoginRequest = {
        username: this.loginForm.value.username,
        password: this.loginForm.value.password
      };

      this.authService.login(loginData).subscribe({
        next: (response) => {

          if(response.status){
            this.sweetAlert.success(response.message,"Đăng nhập")
            this.router.navigate(["home"]);
          }else{
            this.sweetAlert.error(response.message,"Đăng nhập")
            this.isLoading = false;
            return;
          }
          // Handle remember me
          if (this.loginForm.value.rememberMe) {
            this.storageService.setItem('rememberedUsername', loginData.username);
          } else {
            this.storageService.removeItem('rememberedUsername');
          }

          // Redirect to intended page or dashboard
          const redirectUrl = this.storageService.getItem('redirectUrl') || '/home';
          this.storageService.removeItem('redirectUrl');
          this.router.navigate([redirectUrl]);
        },
        error: (error) => {
          this.errorMessage = error.message || 'Đăng nhập thất bại';
          this.isLoading = false;
        }
      });
    } else {
      this.markAllFieldsAsTouched();
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  private markAllFieldsAsTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
  }

  // Getter methods for template
  get username() { return this.loginForm.get('username'); }
  get password() { return this.loginForm.get('password'); }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldErrorMessage(fieldName: string): string {
    const field = this.loginForm.get(fieldName);
    if (field?.hasError('required')) {
      return fieldName === 'username' ? 'Tên đăng nhập là bắt buộc' : 'Mật khẩu là bắt buộc';
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return fieldName === 'username' 
        ? `Tên đăng nhập phải có ít nhất ${minLength} ký tự` 
        : `Mật khẩu phải có ít nhất ${minLength} ký tự`;
    }
    return '';
  }
}
