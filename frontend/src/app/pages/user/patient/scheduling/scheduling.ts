import { Component, afterNextRender, inject, ChangeDetectorRef, LOCALE_ID } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { SearchInputComponent } from '@shared/components/atoms/search-input/search-input';
import { SpecialtiesService, Specialty } from '@core/services/specialties.service';
import { SchedulesService } from '@core/services/schedules.service';
import { UsersService, User } from '@core/services/users.service';
import { ScheduleBlocksService } from '@core/services/schedule-blocks.service';
import { AppointmentsService } from '@core/services/appointments.service';
import { AuthService } from '@core/services/auth.service';
import { forkJoin, Observable, Observer } from 'rxjs';
import localePt from '@angular/common/locales/pt';

registerLocaleData(localePt);

type Step = 'specialty' | 'date' | 'time' | 'professional-selection' | 'confirmation';

interface DayAvailability {
  date: Date;
  slots: number;
  professionalsCount: number;
  available: boolean;
}

interface TimeSlot {
  time: string;
  professionals: User[];
}

@Component({
  selector: 'app-scheduling',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonComponent,
    IconComponent,
    SearchInputComponent
  ],
  providers: [
    { provide: LOCALE_ID, useValue: 'pt-BR' }
  ],
  templateUrl: './scheduling.html',
  styleUrls: ['./scheduling.scss']
})
export class SchedulingComponent {
  currentStep: Step = 'specialty';
  
  steps: { id: Step, label: string }[] = [
    { id: 'specialty', label: 'Especialidade' },
    { id: 'date', label: 'Data' },
    { id: 'time', label: 'Horário' },
    { id: 'professional-selection', label: 'Profissional' },
    { id: 'confirmation', label: 'Confirmação' }
  ];

  weekDays = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

  // Step 1: Specialties
  specialties: Specialty[] = [];
  filteredSpecialties: Specialty[] = [];
  selectedSpecialty: Specialty | null = null;
  searchQuery: string = '';

  // Step 2: Date Selection
  currentMonth: Date = new Date();
  calendarDays: DayAvailability[] = [];
  selectedDate: Date | null = null;

  // Step 3: Time & Professional
  availableSlots: TimeSlot[] = [];
  selectedSlot: TimeSlot | null = null;
  selectedProfessional: User | null = null;
  
  // Step 4: Confirmation
  observation: string = '';
  loading: boolean = false;

  private specialtiesService = inject(SpecialtiesService);
  private schedulesService = inject(SchedulesService);
  private usersService = inject(UsersService);
  private scheduleBlocksService = inject(ScheduleBlocksService);
  private appointmentsService = inject(AppointmentsService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  constructor() {
    afterNextRender(() => {
      this.loadSpecialties();
    });
  }

  getStepIndex(stepId: Step): number {
    return this.steps.findIndex(s => s.id === stepId);
  }

  // --- Step 1: Specialties ---
  loadSpecialties() {
    this.specialtiesService.getSpecialties({ status: 'Active' }).subscribe(response => {
      this.specialties = response.data;
      this.filteredSpecialties = response.data;
      this.cdr.detectChanges();
    });
  }

  onSearch(query: string) {
    this.searchQuery = query;
    this.filteredSpecialties = this.specialties.filter(s => 
      s.name.toLowerCase().includes(query.toLowerCase())
    );
  }

  selectSpecialty(specialty: Specialty) {
    this.selectedSpecialty = specialty;
    this.currentStep = 'date';
    this.generateCalendar();
  }

  // --- Step 2: Date ---
  generateCalendar() {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    this.calendarDays = [];
    
    // Buscar profissionais da especialidade
    this.usersService.getUsers({ role: 'PROFESSIONAL', specialtyId: this.selectedSpecialty?.id }, 1, 100).subscribe({
      next: (response) => {
        const professionals = response.data;
        
        if (professionals.length === 0) {
          // Se não há profissionais, marcar todos os dias como indisponíveis
          for (let i = 1; i <= daysInMonth; i++) {
            const date = new Date(year, month, i);
            this.calendarDays.push({
              date: date,
              slots: 0,
              professionalsCount: 0,
              available: false
            });
          }
          this.cdr.detectChanges();
          return;
        }

        // Para cada dia do mês, verificar bloqueios de todos os profissionais
        const dayChecks: Observable<{ date: Date, availableProfessionals: number }>[] = [];
        
        for (let i = 1; i <= daysInMonth; i++) {
          const date = new Date(year, month, i);
          
          if (date < today) {
            // Dias passados são indisponíveis
            this.calendarDays.push({
              date: date,
              slots: 0,
              professionalsCount: 0,
              available: false
            });
            continue;
          }
          
          // Verificar bloqueios para cada profissional neste dia
          const blockChecks = professionals.map(pro => 
            this.scheduleBlocksService.isDateBlocked(pro.id, date)
          );
          
          dayChecks.push(
            new Observable((observer: Observer<{ date: Date, availableProfessionals: number }>) => {
              forkJoin(blockChecks).subscribe({
                next: (blockedStates) => {
                  const availableCount = blockedStates.filter(blocked => !blocked).length;
                  observer.next({ date, availableProfessionals: availableCount });
                  observer.complete();
                },
                error: () => {
                  // Em caso de erro, assumir que o dia está disponível
                  observer.next({ date, availableProfessionals: professionals.length });
                  observer.complete();
                }
              });
            })
          );
        }
        
        // Processar todos os dias
        if (dayChecks.length > 0) {
          forkJoin(dayChecks).subscribe({
            next: (results) => {
              results.forEach(result => {
                this.calendarDays.push({
                  date: result.date,
                  slots: result.availableProfessionals > 0 ? Math.floor(Math.random() * 5) + 1 : 0,
                  professionalsCount: result.availableProfessionals,
                  available: result.availableProfessionals > 0
                });
              });
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Erro ao verificar bloqueios:', err);
              this.cdr.detectChanges();
            }
          });
        } else {
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Erro ao buscar profissionais:', err);
        // Em caso de erro, gerar calendário básico
        for (let i = 1; i <= daysInMonth; i++) {
          const date = new Date(year, month, i);
          this.calendarDays.push({
            date: date,
            slots: date >= today ? 1 : 0,
            professionalsCount: date >= today ? 1 : 0,
            available: date >= today
          });
        }
        this.cdr.detectChanges();
      }
    });
  }

  changeMonth(delta: number) {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + delta, 1);
    this.generateCalendar();
  }

  selectDate(day: DayAvailability) {
    if (!day.available) return;
    this.selectedDate = day.date;
    this.loadTimeSlots();
    this.currentStep = 'time';
  }

  // --- Step 3: Time & Professional ---
  loadTimeSlots() {
    if (!this.selectedDate || !this.selectedSpecialty) return;

    this.loading = true;
    
    // Buscar profissionais da especialidade
    this.usersService.getUsers({ 
      role: 'PROFESSIONAL', 
      specialtyId: this.selectedSpecialty.id,
      status: 'Active'
    }, 1, 100).subscribe({
      next: (response) => {
        const professionals = response.data;
        
        if (professionals.length === 0) {
          this.availableSlots = [];
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }

        // Buscar disponibilidade de cada profissional para a data selecionada
        const availabilityChecks = professionals.map(pro => 
          this.schedulesService.getAvailability(
            pro.id,
            this.selectedDate!,
            this.selectedDate!
          )
        );

        forkJoin(availabilityChecks).subscribe({
          next: (availabilities) => {
            // Mapear horários disponíveis por profissional
            const timeSlotMap = new Map<string, User[]>();

            availabilities.forEach((availability, index) => {
              const professional = professionals[index];
              
              // A resposta da API tem a estrutura: { professionalId, professionalName, slots: [] }
              if (availability.slots && availability.slots.length > 0) {
                availability.slots.forEach(slot => {
                  // Apenas adicionar slots disponíveis
                  if (slot.isAvailable) {
                    const time = slot.time;
                    if (!timeSlotMap.has(time)) {
                      timeSlotMap.set(time, []);
                    }
                    timeSlotMap.get(time)!.push(professional);
                  }
                });
              }
            });

            // Converter mapa em array de TimeSlot ordenado
            this.availableSlots = Array.from(timeSlotMap.entries())
              .map(([time, profs]) => ({ time, professionals: profs }))
              .sort((a, b) => a.time.localeCompare(b.time));

            this.loading = false;
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Erro ao buscar disponibilidade:', err);
            this.availableSlots = [];
            this.loading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        console.error('Erro ao buscar profissionais:', err);
        this.availableSlots = [];
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectSlot(slot: TimeSlot) {
    this.selectedSlot = slot;
    if (slot.professionals.length === 1) {
      this.selectedProfessional = slot.professionals[0];
      this.currentStep = 'confirmation';
    } else {
      this.currentStep = 'professional-selection';
    }
  }

  selectProfessional(professional: User) {
    this.selectedProfessional = professional;
    this.currentStep = 'confirmation';
  }

  // --- Step 4: Confirmation ---
  confirmScheduling() {
    if (!this.selectedSpecialty || !this.selectedDate || !this.selectedSlot || !this.selectedProfessional) {
      console.error('Dados incompletos para criar agendamento');
      return;
    }

    const currentUser = this.authService.currentUser();
    if (!currentUser) {
      console.error('Usuário não autenticado');
      this.router.navigate(['/entrar']);
      return;
    }

    this.loading = true;

    const appointmentData = {
      patientId: currentUser.id,
      professionalId: this.selectedProfessional.id,
      specialtyId: this.selectedSpecialty.id,
      date: this.selectedDate.toISOString(),
      time: this.selectedSlot.time,
      type: 'Common' as const,
      observation: this.observation || undefined
    };

    this.appointmentsService.createAppointment(appointmentData).subscribe({
      next: (appointment) => {
        this.loading = false;
        this.router.navigate(['/agendamento-sucesso'], { 
          state: { 
            appointment: {
              id: appointment.id,
              specialty: this.selectedSpecialty,
              date: this.selectedDate,
              time: this.selectedSlot?.time,
              professional: this.selectedProfessional,
              observation: this.observation
            }
          }
        });
      },
      error: (err) => {
        console.error('Erro ao criar agendamento:', err);
        this.loading = false;
        alert('Erro ao criar agendamento. Por favor, tente novamente.');
      }
    });
  }

  goToStep(step: Step) {
    const stepOrder: Step[] = ['specialty', 'date', 'time', 'professional-selection', 'confirmation'];
    const currentIndex = stepOrder.indexOf(this.currentStep);
    const targetIndex = stepOrder.indexOf(step);

    // Can only navigate backwards or to the immediate next step if data is selected
    if (targetIndex !== -1 && targetIndex < currentIndex) {
      this.currentStep = step;
      this.resetSelectionsFrom(step);
    }
  }

  resetSelectionsFrom(step: Step) {
    if (step === 'specialty') {
      this.selectedSpecialty = null;
      this.selectedDate = null;
      this.selectedSlot = null;
      this.selectedProfessional = null;
    } else if (step === 'date') {
      this.selectedDate = null;
      this.selectedSlot = null;
      this.selectedProfessional = null;
    } else if (step === 'time') {
      this.selectedSlot = null;
      this.selectedProfessional = null;
    } else if (step === 'professional-selection') {
      this.selectedProfessional = null;
    }
  }

  goBack() {
    if (this.currentStep === 'confirmation') {
        // If we came from professional selection (because >1 pro), go back there.
        // If we came from time slot (because 1 pro), go back to time slot.
        if (this.selectedSlot && this.selectedSlot.professionals.length > 1) {
            this.currentStep = 'professional-selection';
        } else {
            this.currentStep = 'time';
            this.selectedProfessional = null;
        }
    } else if (this.currentStep === 'professional-selection') {
        this.currentStep = 'time';
        this.selectedProfessional = null;
        this.selectedSlot = null;
    } else if (this.currentStep === 'time') {
        this.currentStep = 'date';
        this.selectedDate = null;
        this.selectedSlot = null;
    } else if (this.currentStep === 'date') {
        this.currentStep = 'specialty';
        this.selectedSpecialty = null;
    }
  }
}
