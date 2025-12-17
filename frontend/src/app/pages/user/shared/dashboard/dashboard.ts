import { Component, OnInit, ElementRef, ViewChild, AfterViewInit, effect, PLATFORM_ID, Inject } from '@angular/core';
import { CommonModule, DatePipe, isPlatformBrowser } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AvatarComponent } from '@app/shared/components/atoms/avatar/avatar';
import { BadgeComponent } from '@app/shared/components/atoms/badge/badge';
import { StatCardComponent } from '@app/shared/components/atoms/stat-card/stat-card';
import { IconComponent } from '@app/shared/components/atoms/icon/icon';
import { ButtonComponent } from '@app/shared/components/atoms/button/button';
import { StatsService, PlatformStats } from '@app/core/services/stats.service';
import { AuthService } from '@app/core/services/auth.service';
import { AppointmentsService, Appointment } from '@app/core/services/appointments.service';
import { NotificationsService, Notification } from '@app/core/services/notifications.service';
import { User as AuthUser } from '@app/core/models/auth.model';
import Chart from 'chart.js/auto';

interface DashboardUser {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: string;
  memberSince: string;
  lastAccess: string;
}

interface ScheduleBlock {
  id: number;
  type: 'single' | 'range';
  date?: string;
  startDate?: string;
  endDate?: string;
  reason: string;
  status: 'pendente' | 'aprovada' | 'negada' | 'vencido';
  createdAt: string;
}

type ViewMode = 'admin' | 'patient' | 'professional';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    AvatarComponent,
    BadgeComponent,
    StatCardComponent,
    IconComponent,
    ButtonComponent
  ],
  providers: [DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit, AfterViewInit {
  user: DashboardUser | null = null;
  stats: PlatformStats | null = null;
  nextAppointments: Appointment[] = [];
  notifications: Notification[] = [];
  scheduleBlocks: ScheduleBlock[] = [];
  viewMode: ViewMode = 'patient'; // Default
  
  @ViewChild('appointmentsChart') appointmentsChartRef!: ElementRef;
  @ViewChild('usersChart') usersChartRef!: ElementRef;
  @ViewChild('monthlyChart') monthlyChartRef!: ElementRef;

  appointmentsChart: any;
  usersChart: any;
  monthlyChart: any;

  constructor(
    private statsService: StatsService,
    private authService: AuthService,
    private appointmentsService: AppointmentsService,
    private notificationsService: NotificationsService,
    private datePipe: DatePipe,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    effect(() => {
      const authUser = this.authService.currentUser();
      if (authUser) {
        this.updateUser(authUser);
      }
    });
  }

  ngOnInit(): void {
    this.determineViewMode();

    // Initial load attempt (in case signal is already set)
    const authUser = this.authService.currentUser();
    if (authUser) {
      this.updateUser(authUser);
    }
  }

  ngAfterViewInit(): void {
    // We'll handle chart loading in the updateUser method or here if user is already loaded
    if (this.isAdmin()) {
      this.loadCharts();
    }
  }

  private determineViewMode(): void {
    const url = this.router.url;
    if (url.includes('/admin')) {
      this.viewMode = 'admin';
    } else if (url.includes('/professional')) {
      this.viewMode = 'professional';
    } else {
      this.viewMode = 'patient';
    }
  }

  private updateUser(authUser: AuthUser) {
    this.user = {
      id: authUser.id,
      name: authUser.name,
      email: authUser.email,
      avatar: authUser.avatar,
      role: authUser.role,
      memberSince: this.formatDate(authUser.createdAt),
      lastAccess: 'Hoje às ' + new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    };

    // Load data based on VIEW MODE, not just user role
    if (this.isAdmin()) {
      this.loadStats();
      // If view is already initialized, we might need to load charts
      if (this.appointmentsChartRef) {
        this.loadCharts();
      }
    } else if (this.isProfessional()) {
      this.loadScheduleBlocks();
      this.loadNotifications();
      this.loadNextAppointments();
    } else {
      this.loadNotifications();
      this.loadNextAppointments();
    }
  }

  isAdmin(): boolean {
    return this.viewMode === 'admin';
  }

  isPatient(): boolean {
    return this.viewMode === 'patient';
  }

  isProfessional(): boolean {
    return this.viewMode === 'professional';
  }

  getUserRolePath(): string {
    return this.viewMode;
  }

  private formatDate(date: Date | string): string {
    return this.datePipe.transform(date, 'MMMM yyyy') || '';
  }

  private loadNextAppointments(): void {
    this.appointmentsService.getAppointments({ status: 'upcoming' }).subscribe({
      next: (appointments) => {
        // Take top 3 upcoming
        this.nextAppointments = appointments.slice(0, 3);
      },
      error: (err) => console.error('Erro ao carregar consultas', err)
    });
  }

  private loadNotifications(): void {
    this.notificationsService.getNotifications().subscribe({
      next: (notifications) => {
        // Take top 3
        this.notifications = notifications.slice(0, 3);
      },
      error: (err) => console.error('Erro ao carregar notificações', err)
    });
  }

  private loadScheduleBlocks(): void {
    // Mock data for professional schedule blocks
    this.scheduleBlocks = [
      {
        id: 1,
        type: 'single',
        date: new Date(new Date().setDate(new Date().getDate() + 5)).toISOString(),
        reason: 'Consulta Médica',
        status: 'pendente',
        createdAt: new Date().toISOString()
      },
      {
        id: 2,
        type: 'range',
        startDate: new Date(new Date().setDate(new Date().getDate() + 10)).toISOString(),
        endDate: new Date(new Date().setDate(new Date().getDate() + 12)).toISOString(),
        reason: 'Congresso',
        status: 'aprovada',
        createdAt: new Date(new Date().setDate(new Date().getDate() - 2)).toISOString()
      }
    ];
  }

  private loadStats(): void {
    this.statsService.getPlatformStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Erro ao carregar estatísticas:', error);
      }
    });
  }

  private loadCharts(): void {
    // Only load charts in the browser, not during SSR
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    // Wrap in timeout to ensure DOM is ready if switching views
    setTimeout(() => {
        if (!this.appointmentsChartRef || !this.usersChartRef || !this.monthlyChartRef) return;
        
        // Destroy existing charts if any to avoid duplicates/errors
        if (this.appointmentsChart) this.appointmentsChart.destroy();
        if (this.usersChart) this.usersChart.destroy();
        if (this.monthlyChart) this.monthlyChart.destroy();

        this.statsService.getAppointmentsByStatus().subscribe(data => {
          if (this.appointmentsChartRef) {
            this.appointmentsChart = new Chart(this.appointmentsChartRef.nativeElement, {
              type: 'doughnut',
              data: data,
              options: {
                responsive: true,
                plugins: {
                  legend: { position: 'bottom' }
                }
              }
            });
          }
        });

        this.statsService.getUsersByRole().subscribe(data => {
          if (this.usersChartRef) {
            this.usersChart = new Chart(this.usersChartRef.nativeElement, {
              type: 'bar',
              data: data,
              options: {
                responsive: true,
                scales: {
                  y: { beginAtZero: true }
                },
                plugins: {
                  legend: { display: false }
                }
              }
            });
          }
        });

        this.statsService.getMonthlyAppointments().subscribe(data => {
          if (this.monthlyChartRef) {
            this.monthlyChart = new Chart(this.monthlyChartRef.nativeElement, {
              type: 'line',
              data: data,
              options: {
                responsive: true,
                scales: {
                  y: { beginAtZero: true }
                },
                plugins: {
                  legend: { position: 'top' }
                }
              }
            });
          }
        });
    }, 0);
  }
}
