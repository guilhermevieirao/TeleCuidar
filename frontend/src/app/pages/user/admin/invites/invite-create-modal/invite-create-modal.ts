import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';
import { UserRole } from '@app/core/services/invites.service';

export type InviteAction = 'send-email' | 'generate-link';

@Component({
  selector: 'app-invite-create-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, ButtonComponent],
  templateUrl: './invite-create-modal.html',
  styleUrl: './invite-create-modal.scss'
})
export class InviteCreateModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() create = new EventEmitter<{ email: string; role: UserRole; action: InviteAction }>();

  inviteData = {
    email: '',
    role: 'PATIENT' as UserRole
  };

  roleOptions = [
    { value: 'PATIENT', label: 'Paciente' },
    { value: 'PROFESSIONAL', label: 'Profissional' },
    { value: 'ADMIN', label: 'Administrador' }
  ];

  onBackdropClick(): void {
    this.onCancel();
  }

  onCancel(): void {
    this.resetModal();
    this.close.emit();
  }

  onSendEmail(): void {
    if (this.isFormValidForEmail()) {
      this.create.emit({ ...this.inviteData, action: 'send-email' });
      this.resetModal();
    }
  }

  onGenerateLink(): void {
    if (this.isFormValidForLink()) {
      this.create.emit({ ...this.inviteData, action: 'generate-link' });
      this.resetModal();
    }
  }

  isFormValidForEmail(): boolean {
    return !!(
      this.inviteData.email?.trim() &&
      this.isValidEmail(this.inviteData.email)
    );
  }

  isFormValidForLink(): boolean {
    return !!this.inviteData.role;
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  private resetModal(): void {
    this.inviteData = {
      email: '',
      role: 'PATIENT'
    };
  }
}
