import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';
import { AvatarUploadComponent } from '@app/shared/components/molecules/avatar-upload/avatar-upload';
import { CpfMaskDirective } from '@app/core/directives/cpf-mask.directive';
import { PhoneMaskDirective } from '@app/core/directives/phone-mask.directive';
import { EmailValidatorDirective } from '@app/core/directives/email-validator.directive';
import { User } from '@app/core/services/users.service';

@Component({
  selector: 'app-profile-edit-modal',
  imports: [FormsModule, IconComponent, ButtonComponent, AvatarUploadComponent, CpfMaskDirective, PhoneMaskDirective, EmailValidatorDirective],
  templateUrl: './profile-edit-modal.html',
  styleUrl: './profile-edit-modal.scss'
})
export class ProfileEditModalComponent implements OnChanges {
  @Input() user: User | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<Partial<User>>();
  @Output() changePassword = new EventEmitter<void>();

  editedUser: Partial<User> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['user'] && this.user) {
      this.editedUser = { ...this.user };
    }
  }

  onBackdropClick(): void {
    this.onCancel();
  }

  onCancel(): void {
    this.close.emit();
  }

  onSave(): void {
    if (this.editedUser && this.isFormValid()) {
      this.save.emit(this.editedUser);
    }
  }

  onAvatarChange(avatarUrl: string): void {
    this.editedUser.avatar = avatarUrl;
  }

  isFormValid(): boolean {
    return !!(
      this.editedUser.name?.trim() &&
      this.editedUser.email?.trim() &&
      this.editedUser.cpf?.trim() &&
      this.editedUser.phone?.trim()
    );
  }

  onChangePassword(): void {
    this.changePassword.emit();
  }
}
