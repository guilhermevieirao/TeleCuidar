import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { 
  NotificationsService, 
  Notification, 
  NotificationType, 
  NotificationStatus 
} from '@app/core/services/notifications.service';

@Component({
  selector: 'app-notifications',
  imports: [FormsModule, IconComponent],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class NotificationsComponent implements OnInit {
  notifications: Notification[] = [];
  statusFilter: 'all' | NotificationStatus = 'all';
  typeFilter: 'all' | NotificationType = 'all';
  loading = false;
  unreadCount = 0;

  constructor(private notificationsService: NotificationsService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  get hasUnreadNotifications(): boolean {
    return this.unreadCount > 0;
  }

  setStatusFilter(status: 'all' | NotificationStatus): void {
    this.statusFilter = status;
    this.loadNotifications();
  }

  setTypeFilter(type: 'all' | NotificationType): void {
    this.typeFilter = type;
    this.loadNotifications();
  }

  onFilterChange(): void {
    this.loadNotifications();
  }

  markAsRead(notificationId: string): void {
    this.notificationsService.markAsRead(notificationId).subscribe({
      next: () => {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification) {
          notification.status = 'read';
          this.updateUnreadCount();
        }
      },
      error: (error: Error) => {
        console.error('Erro ao marcar notificação como lida:', error);
      }
    });
  }

  markAllAsRead(): void {
    this.notificationsService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.status = 'read');
        this.updateUnreadCount();
      },
      error: (error: Error) => {
        console.error('Erro ao marcar todas como lidas:', error);
      }
    });
  }

  deleteNotification(notificationId: string): void {
    this.notificationsService.deleteNotification(notificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(n => n.id !== notificationId);
        this.updateUnreadCount();
      },
      error: (error: Error) => {
        console.error('Erro ao excluir notificação:', error);
      }
    });
  }

  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Agora';
    if (diffMins < 60) return `${diffMins} min atrás`;
    if (diffHours < 24) return `${diffHours}h atrás`;
    if (diffDays < 7) return `${diffDays}d atrás`;

    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private loadNotifications(): void {
    this.loading = true;
    this.notificationsService.getNotifications({
      status: this.statusFilter,
      type: this.typeFilter
    }).subscribe({
      next: (notifications: Notification[]) => {
        this.notifications = notifications;
        this.updateUnreadCount();
        this.loading = false;
      },
      error: (error: Error) => {
        console.error('Erro ao carregar notificações:', error);
        this.loading = false;
      }
    });
  }

  private updateUnreadCount(): void {
    this.unreadCount = this.notifications.filter(n => n.status === 'unread').length;
  }
}
