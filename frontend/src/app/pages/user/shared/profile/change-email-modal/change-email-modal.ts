import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';
import { EmailValidatorDirective } from '@app/core/directives/email-validator.directive';

@Component({
  selector: 'app-change-email-modal',
  imports: [FormsModule, IconComponent, ButtonComponent, EmailValidatorDirective],
  templateUrl: './change-email-modal.html',
  styleUrl: './change-email-modal.scss'
})
export class ChangeEmailModalComponent {
  @Input() isOpen = false;
  @Input() currentEmail = '';
  @Input() isLoading = false;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<string>();

  newEmail = '';
  confirmEmail = '';

  onBackdropClick(): void {
    if (!this.isLoading) {
      this.onCancel();
    }
  }

  onCancel(): void {
    this.newEmail = '';
    this.confirmEmail = '';
    this.close.emit();
  }

  onSave(): void {
    if (this.isFormValid()) {
      this.save.emit(this.newEmail);
    }
  }

  isFormValid(): boolean {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return !!(
      this.newEmail.trim() &&
      this.confirmEmail.trim() &&
      emailRegex.test(this.newEmail) &&
      this.newEmail === this.confirmEmail &&
      this.newEmail !== this.currentEmail
    );
  }

  getEmailError(): string {
    if (!this.newEmail.trim()) {
      return '';
    }
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (!emailRegex.test(this.newEmail)) {
      return 'E-mail inválido';
    }
    if (this.newEmail === this.currentEmail) {
      return 'O novo e-mail deve ser diferente do atual';
    }
    return '';
  }

  getConfirmError(): string {
    if (!this.confirmEmail.trim()) {
      return '';
    }
    if (this.newEmail !== this.confirmEmail) {
      return 'Os e-mails não coincidem';
    }
    return '';
  }
}
