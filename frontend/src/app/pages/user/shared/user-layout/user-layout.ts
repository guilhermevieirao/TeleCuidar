import { Component, OnInit, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser, TitleCasePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LogoComponent } from '@app/shared/components/atoms/logo/logo';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { AvatarComponent } from '@app/shared/components/atoms/avatar/avatar';
import { ThemeToggleComponent } from '@app/shared/components/atoms/theme-toggle/theme-toggle';

interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: 'admin' | 'professional' | 'patient';
}

interface Notification {
  id: string;
  title: string;
  message: string;
  link?: string;
  isRead: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    LogoComponent,
    IconComponent,
    AvatarComponent,
    ThemeToggleComponent,
    TitleCasePipe
  ],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.scss'
})
export class UserLayoutComponent implements OnInit {
  isSidebarOpen = false;
  isNotificationDropdownOpen = false;
  unreadNotifications = 0;
  user: User | null = null;
  notifications: Notification[] = [];
  basePath = '';

  constructor(
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    this.basePath = this.getBasePath();
    this.loadUserData();
    this.loadUnreadNotifications();
    this.loadNotifications();
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleNotificationDropdown(): void {
    this.isNotificationDropdownOpen = !this.isNotificationDropdownOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }

  closeSidebarOnMobile(): void {
    if (isPlatformBrowser(this.platformId)) {
      if (window.innerWidth < 768) {
        this.closeSidebar();
      }
    }
  }

  logout(): void {
    // TODO: Implementar logout com backend
    this.router.navigate(['/auth/login']);
  }

  private getBasePath(): string {
    const urlSegments = this.router.url.split('/');
    if (urlSegments.length > 1) {
      return urlSegments[1];
    }
    return 'admin'; // Fallback
  }

  private loadUserData(): void {
    // TODO: Integrar com serviço de autenticação
    const role = this.basePath as 'admin' | 'professional' | 'patient';
    this.user = {
      id: '1',
      name: `${role.charAt(0).toUpperCase() + role.slice(1)} User`,
      email: `${role}@telecuidar.com.br`,
      role: role
    };
  }

  private loadUnreadNotifications(): void {
    // TODO: Integrar com serviço de notificações
    this.unreadNotifications = 5;
  }

  private loadNotifications(): void {
    const basePath = this.getBasePath();
    // TODO: Integrar com serviço de notificações
    this.notifications = [
      {
        id: '1',
        title: 'Nova mensagem',
        message: 'Você recebeu uma nova mensagem do paciente João Silva',
        link: `/${basePath}/notifications`,
        isRead: false,
        createdAt: new Date(Date.now() - 5 * 60000).toISOString()
      },
      {
        id: '2',
        title: 'Consulta confirmada',
        message: 'A consulta de Maria Santos foi confirmada para 15/12/2025',
        link: `/${basePath}/notifications`,
        isRead: false,
        createdAt: new Date(Date.now() - 15 * 60000).toISOString()
      },
      {
        id: '3',
        title: 'Aviso de sistema',
        message: 'Sistema será atualizado em 2 horas',
        link: `/${basePath}/notifications`,
        isRead: true,
        createdAt: new Date(Date.now() - 1 * 3600000).toISOString()
      },
      {
        id: '4',
        title: 'Novo usuário registrado',
        message: 'Um novo profissional se registrou no sistema',
        link: `/${basePath}/notifications`,
        isRead: true,
        createdAt: new Date(Date.now() - 2 * 3600000).toISOString()
      },
      {
        id: '5',
        title: 'Agendamento cancelado',
        message: 'Agendamento do paciente Pedro Santos foi cancelado',
        link: `/${basePath}/notifications`,
        isRead: true,
        createdAt: new Date(Date.now() - 6 * 3600000).toISOString()
      }
    ];
  }

  formatNotificationTime(date: string): string {
    const notificationDate = new Date(date);
    const now = new Date();
    const diffMs = now.getTime() - notificationDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Agora';
    if (diffMins < 60) return `há ${diffMins}m`;
    if (diffHours < 24) return `há ${diffHours}h`;
    if (diffDays < 7) return `há ${diffDays}d`;

    return notificationDate.toLocaleDateString('pt-BR');
  }

  markAllAsRead(): void {
    // TODO: Integrar com serviço de notificações
    this.notifications = this.notifications.map(notification => ({
      ...notification,
      isRead: true
    }));
    this.unreadNotifications = 0;
  }
}
