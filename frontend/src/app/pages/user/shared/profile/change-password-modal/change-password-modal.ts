import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';

@Component({
  selector: 'app-change-password-modal',
  imports: [FormsModule, IconComponent, ButtonComponent],
  templateUrl: './change-password-modal.html',
  styleUrl: './change-password-modal.scss'
})
export class ChangePasswordModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<{
    currentPassword: string;
    newPassword: string;
  }>();

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  onBackdropClick(): void {
    this.onCancel();
  }

  onCancel(): void {
    this.resetForm();
    this.close.emit();
  }

  onSave(): void {
    if (this.isFormValid()) {
      this.save.emit({
        currentPassword: this.currentPassword,
        newPassword: this.newPassword
      });
      this.resetForm();
    }
  }

  toggleCurrentPasswordVisibility(): void {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  isFormValid(): boolean {
    return !!(
      this.currentPassword.trim() &&
      this.newPassword.trim() &&
      this.confirmPassword.trim() &&
      this.newPassword === this.confirmPassword &&
      this.newPassword.length >= 8
    );
  }

  passwordsMatch(): boolean {
    if (!this.confirmPassword) return true;
    return this.newPassword === this.confirmPassword;
  }

  isPasswordStrong(): boolean {
    if (!this.newPassword) return true;
    
    const hasMinLength = this.newPassword.length >= 8;
    const hasUpperCase = /[A-Z]/.test(this.newPassword);
    const hasLowerCase = /[a-z]/.test(this.newPassword);
    const hasNumber = /[0-9]/.test(this.newPassword);
    
    return hasMinLength && hasUpperCase && hasLowerCase && hasNumber;
  }

  getPasswordStrengthMessage(): string {
    if (!this.newPassword) return '';
    
    const hasMinLength = this.newPassword.length >= 8;
    const hasUpperCase = /[A-Z]/.test(this.newPassword);
    const hasLowerCase = /[a-z]/.test(this.newPassword);
    const hasNumber = /[0-9]/.test(this.newPassword);
    
    if (hasMinLength && hasUpperCase && hasLowerCase && hasNumber) {
      return 'Senha forte';
    }
    
    const missing: string[] = [];
    if (!hasMinLength) missing.push('8 caracteres');
    if (!hasUpperCase) missing.push('letra maiúscula');
    if (!hasLowerCase) missing.push('letra minúscula');
    if (!hasNumber) missing.push('número');
    
    return `Faltam: ${missing.join(', ')}`;
  }

  private resetForm(): void {
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.showCurrentPassword = false;
    this.showNewPassword = false;
    this.showConfirmPassword = false;
  }
}
