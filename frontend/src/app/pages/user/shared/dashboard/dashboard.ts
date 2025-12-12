import { Component, OnInit } from '@angular/core';
import { AvatarComponent } from '@app/shared/components/atoms/avatar/avatar';
import { BadgeComponent } from '@app/shared/components/atoms/badge/badge';
import { StatCardComponent } from '@app/shared/components/atoms/stat-card/stat-card';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { StatsService, PlatformStats } from '@app/core/services/stats.service';

interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: 'admin' | 'professional' | 'patient';
  memberSince: string;
  lastAccess: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [
    AvatarComponent,
    BadgeComponent,
    StatCardComponent,
    IconComponent
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  user: User | null = null;
  stats: PlatformStats | null = null;

  constructor(private statsService: StatsService) {}

  ngOnInit(): void {
    this.loadUserData();
    this.loadStats();
  }

  private loadUserData(): void {
    // TODO: Integrar com serviço de autenticação real
    this.user = {
      id: '1',
      name: 'Admin User',
      email: 'admin@telecuidar.com.br',
      role: 'admin',
      memberSince: 'Janeiro 2024',
      lastAccess: 'Hoje às 14:30'
    };
  }

  private loadStats(): void {
    this.statsService.getPlatformStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Erro ao carregar estatísticas:', error);
        // TODO: Mostrar mensagem de erro para o usuário
      }
    });
  }
}
