import { Component, OnInit } from '@angular/core';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { AvatarComponent } from '@app/shared/components/atoms/avatar/avatar';
import { ProfileEditModalComponent } from '@pages/user/shared/profile/profile-edit-modal/profile-edit-modal';
import { ChangePasswordModalComponent } from '@pages/user/shared/profile/change-password-modal/change-password-modal';
import { User } from '@app/core/services/users.service';
import { DatePipe } from '@angular/common';
import { BadgeComponent } from '@app/shared/components/atoms/badge/badge';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';
import { ModalService } from '@app/core/services/modal.service';

@Component({
  selector: 'app-profile',
  imports: [IconComponent, AvatarComponent, ProfileEditModalComponent, ChangePasswordModalComponent, DatePipe, BadgeComponent, ButtonComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class ProfileComponent implements OnInit {
  user: User | null = null;
  emailVerified: boolean = false;
  isSendingVerification = false;
  isEditModalOpen = false;
  isChangePasswordModalOpen = false;

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    // Mock do usuário logado - em produção virá do AuthService
    this.user = {
      id: '3',
      name: 'Pedro Costa',
      email: 'pedro.costa@email.com',
      role: 'admin',
      cpf: '345.678.901-22',
      phone: '(11) 98765-4323',
      status: 'active',
      createdAt: '2024-03-10T09:15:00',
      avatar: undefined,
      emailVerified: false // mock
    };
    this.emailVerified = !!this.user.emailVerified;
  }

  openEditModal(): void {
    this.isEditModalOpen = true;
  }

  onEditModalClose(): void {
    this.isEditModalOpen = false;
  }

  onProfileUpdated(updatedUser: Partial<User>): void {
    if (this.user) {
      this.user = { ...this.user, ...updatedUser };
      this.emailVerified = !!this.user.emailVerified;
      this.isEditModalOpen = false;
    }
  }

  getEmailVerificationLabel(): string {
    return this.emailVerified ? 'Verificado' : 'Não verificado';
  }

  getEmailVerificationVariant(): 'success' | 'warning' {
    return this.emailVerified ? 'success' : 'warning';
  }

  resendVerificationEmail(): void {
    if (!this.user) return;
    this.isSendingVerification = true;
    this.modalService.confirm({
      title: 'Reenviar verificação',
      message: `Deseja reenviar o e-mail de verificação para ${this.user.email}?`,
      confirmText: 'Reenviar',
      cancelText: 'Cancelar',
      variant: 'info'
    }).subscribe(result => {
      if (result.confirmed) {
        setTimeout(() => {
          this.isSendingVerification = false;
          this.modalService.alert({
            title: 'Verificação enviada',
            message: 'E-mail de verificação enviado com sucesso!',
            variant: 'success'
          });
        }, 1200);
      } else {
        this.isSendingVerification = false;
      }
    });
  }

  getRoleLabel(role: string): string {
    const labels: Record<string, string> = {
      patient: 'Paciente',
      professional: 'Profissional',
      admin: 'Administrador'
    };
    return labels[role] || role;
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      active: 'Ativo',
      inactive: 'Inativo'
    };
    return labels[status] || status;
  }

  onChangePassword(): void {
    this.isEditModalOpen = false;
    this.isChangePasswordModalOpen = true;
  }

  onChangePasswordModalClose(): void {
    this.isChangePasswordModalOpen = false;
  }

  onPasswordChanged(data: { currentPassword: string; newPassword: string }): void {
    // TODO: Integrar com serviço de autenticação
    console.log('Trocar senha:', { currentPassword: '***', newPassword: '***' });
    
    // Simular sucesso
    alert('Senha alterada com sucesso!');
    this.isChangePasswordModalOpen = false;
  }
}
