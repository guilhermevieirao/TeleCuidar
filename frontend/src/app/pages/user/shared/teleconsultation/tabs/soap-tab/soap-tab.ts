import { Component, Input, OnInit, OnDestroy, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { AppointmentsService, Appointment } from '@core/services/appointments.service';
import { TeleconsultationRealTimeService, DataUpdatedEvent } from '@core/services/teleconsultation-realtime.service';
import { Subject, takeUntil, debounceTime } from 'rxjs';

@Component({
  selector: 'app-soap-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonComponent],
  templateUrl: './soap-tab.html',
  styleUrls: ['./soap-tab.scss']
})
export class SoapTabComponent implements OnInit, OnDestroy, OnChanges {
  @Input() appointmentId: string | null = null;
  @Input() appointment: Appointment | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PATIENT';
  @Input() readonly = false;

  soapForm: FormGroup;
  isSaving = false;
  lastSaved: Date | null = null;
  private destroy$ = new Subject<void>();
  private dataLoaded = false;
  private isReceivingUpdate = false; // Flag para evitar loop de atualizações

  constructor(
    private fb: FormBuilder,
    private appointmentsService: AppointmentsService,
    private teleconsultationRealTime: TeleconsultationRealTimeService,
    private cdr: ChangeDetectorRef
  ) {
    this.soapForm = this.fb.group({
      subjective: [''],
      objective: [''],
      assessment: [''],
      plan: ['']
    });
  }

  ngOnInit() {
    // Setup real-time subscriptions
    this.setupRealTimeSubscriptions();
    
    if (this.readonly) {
      this.soapForm.disable();
      return;
    }
    
    // Carregar dados existentes do SOAP se já tiver appointment
    if (this.appointment && !this.dataLoaded) {
      this.loadSoapData();
    }
    
    // Auto-save on value changes (debounced) - apenas para profissionais
    if (this.userrole === 'PROFESSIONAL') {
      // Enviar preview em tempo real (debounce curto)
      this.soapForm.valueChanges
        .pipe(
          takeUntil(this.destroy$),
          debounceTime(300) // 300ms para preview em tempo real
        )
        .subscribe(() => {
          if (!this.isReceivingUpdate && this.appointmentId) {
            // Salvar no cache local
            this.saveToLocalCache();
            // Notificar outros participantes sobre a mudança (preview)
            this.teleconsultationRealTime.notifyDataUpdated(
              this.appointmentId,
              'soap',
              this.soapForm.value
            );
          }
        });
      
      // Salvar no banco com debounce maior
      this.soapForm.valueChanges
        .pipe(
          takeUntil(this.destroy$),
          debounceTime(2000) // 2s para salvar no banco
        )
        .subscribe(() => {
          if (this.soapForm.dirty && !this.isReceivingUpdate) {
            this.saveSoap();
          }
        });
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    // Quando appointment mudar e ainda não carregamos os dados, carregar
    if (changes['appointment'] && changes['appointment'].currentValue && !this.dataLoaded) {
      this.loadSoapData();
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupRealTimeSubscriptions(): void {
    // Listen for SOAP updates from other participant
    this.teleconsultationRealTime.getDataUpdates$('soap')
      .pipe(takeUntil(this.destroy$))
      .subscribe((event: DataUpdatedEvent) => {
        if (event.data) {
          // Marcar que estamos recebendo uma atualização para evitar loop
          this.isReceivingUpdate = true;
          
          // Update form with received data
          this.soapForm.patchValue(event.data, { emitEvent: false });
          this.soapForm.markAsPristine();
          this.cdr.detectChanges();
          
          // Liberar flag após um pequeno delay
          setTimeout(() => {
            this.isReceivingUpdate = false;
          }, 100);
        }
      });
  }

  loadSoapData() {
    // Primeiro verificar se há dados no cache local (mais recentes)
    const cachedData = this.loadFromLocalCache();
    if (cachedData) {
      this.soapForm.patchValue(cachedData, { emitEvent: false });
      this.soapForm.markAsPristine();
      this.dataLoaded = true;
      return;
    }
    
    if (this.appointment?.soapJson) {
      try {
        const soapData = JSON.parse(this.appointment.soapJson);
        this.soapForm.patchValue(soapData, { emitEvent: false });
        this.soapForm.markAsPristine();
        this.dataLoaded = true;
      } catch (error) {
        console.error('Erro ao carregar dados do SOAP:', error);
      }
    } else if (this.appointment) {
      // Appointment existe mas não tem dados de SOAP ainda
      this.dataLoaded = true;
    }
  }

  private getCacheKey(): string {
    return `soap_${this.appointmentId}`;
  }

  private saveToLocalCache(): void {
    if (!this.appointmentId) return;
    try {
      sessionStorage.setItem(this.getCacheKey(), JSON.stringify(this.soapForm.value));
    } catch (e) {
      // Ignorar erros de sessionStorage
    }
  }

  private loadFromLocalCache(): any {
    if (!this.appointmentId) return null;
    try {
      const cached = sessionStorage.getItem(this.getCacheKey());
      return cached ? JSON.parse(cached) : null;
    } catch (e) {
      return null;
    }
  }

  private clearLocalCache(): void {
    if (!this.appointmentId) return;
    try {
      sessionStorage.removeItem(this.getCacheKey());
    } catch (e) {
      // Ignorar erros de sessionStorage
    }
  }

  saveSoap() {
    if (!this.appointmentId) return;
    
    this.isSaving = true;
    
    const soapJson = JSON.stringify(this.soapForm.value);
    
    this.appointmentsService.updateAppointment(this.appointmentId, { soapJson })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.lastSaved = new Date();
          this.soapForm.markAsPristine();
          // Limpar cache após salvar com sucesso
          this.clearLocalCache();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Erro ao salvar SOAP:', error);
          this.isSaving = false;
          this.cdr.detectChanges();
        }
      });
  }
}
