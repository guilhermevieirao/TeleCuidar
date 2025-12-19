import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { Specialty } from '@core/services/specialties.service';
import { User } from '@core/services/users.service';

interface AppointmentDetails {
  id?: string;
  specialty: Specialty;
  date: string;
  time: string;
  professional: User;
  observation?: string;
}

@Component({
  selector: 'app-scheduling-success',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonComponent,
    IconComponent
  ],
  templateUrl: './scheduling-success.html',
  styleUrls: ['./scheduling-success.scss']
})
export class SchedulingSuccessComponent implements OnInit {
  appointment: AppointmentDetails | null = null;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras.state) {
      this.appointment = navigation.extras.state['appointment'] as AppointmentDetails;
    }
  }

  ngOnInit(): void {
    if (!this.appointment) {
      // Redirect back if no state (e.g. direct access)
      this.router.navigate(['/painel']);
    }
  }

  goToDashboard() {
    this.router.navigate(['/painel']);
  }

  goToAppointments() {
    this.router.navigate(['/consultas']);
  }

  goToPreConsultation() {
    if (this.appointment?.id) {
        this.router.navigate(['/consultas', this.appointment.id, 'pre-consulta']);
    } else {
        // Fallback if ID is missing (should not happen in real scenario if passed correctly)
        this.goToAppointments();
    }
  }
}
